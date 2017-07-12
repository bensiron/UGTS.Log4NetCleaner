using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using NUnit.Framework;
using UGTS.Testing;

namespace UGTS.Log4Net.Extensions.FunctionalTest
{
    [TestFixture]
    public class SelfCleaningRollingFileAppenderTests
    {
        private string _testLogPath;
        private Logger _logger;
        private PatternLayout _layout;
        
        [SetUp]
        public void Setup()
        {
            _testLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp\\Log4Net.Extensions");
            ResetLogging();
            DeleteFiles();
            Directory.CreateDirectory(_testLogPath);

            _logger = ((Hierarchy)LogManager.GetRepository()).Root;
            _logger.Level = Level.All;
            _layout = new PatternLayout();
            _layout.ActivateOptions();
        }

        [TearDown]
        public void Teardown()
        {
            ResetLogging();
            DeleteFiles();
        }

        private SelfCleaningRollingFileAppender SetAppender(Func<SelfCleaningRollingFileAppender> generator)
        {
            var appender = generator();

            appender.Layout = _layout;
            appender.AppendToFile = true;
            appender.StaticLogFileName = false;
            appender.File = _testLogPath + "\\";

            appender.ActivateOptions();
            _logger.AddAppender(appender);
            _logger.Repository.Configured = true;

            return appender;
        }

        [Test]
        public void Creates_Cleaning_Check_File_At_First_Log_Call()
        {
            SetAppender(() => new SelfCleaningRollingFileAppender { Cleaner = new SelfCleaner { MaximumFileAgeDays = 1 }});

            VerifyFileDoesNotExist(CleaningCheckPath);

            var beforeLogging = DateTime.UtcNow;
            _logger.Log(Level.Debug, "checking...", null);
            var afterLogging = DateTime.UtcNow;

            VerifyFileExists(CleaningCheckPath);
            var checkTime = File.GetLastWriteTimeUtc(CleaningCheckPath);
            Assert.That(checkTime, Is.GreaterThanOrEqualTo(beforeLogging));
            Assert.That(checkTime, Is.LessThanOrEqualTo(afterLogging));
        }

        [Test]
        public void Cleans_Up_Old_Files()
        {
            const string path1 = "sub\\a.txt";
            const string path2 = "sub\\sub\\b.txt";
            const string path3 = "sub\\other\\c.txt";
            CreateEmptyFile(path1, DateTime.UtcNow.AddDays(-3));
            CreateEmptyFile(path2, DateTime.UtcNow.AddDays(-2));
            CreateEmptyFile(path3, DateTime.UtcNow.AddDays(-1));

            SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Date,
                Cleaner = new SelfCleaner { MaximumFileAgeDays = 1.5 },
                DatePattern = "dd_MM_yyyy'.txt'"
            });

            _logger.Log(Level.Debug, "removing old files", null);

            VerifyFileCount(3);
            VerifyFileDoesNotExist(path1);
            VerifyDirectoryExists("sub");
            VerifyFileDoesNotExist(path2);
            VerifyDirectoryDoesNotExist("sub\\sub");
            VerifyFileExists(path3);
            VerifyDirectoryExists("sub\\other");
            VerifyFileExists(CleaningCheckPath);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void Backup_Files_Also_Get_Cleaned(int countDirection)
        {
            var appender = SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Composite,
                DatePattern = "dd_MM_yyyy'.txt'",
                MaxSizeRollBackups = 1,
                CountDirection = countDirection,
                Cleaner = new SelfCleaner { MaximumDirectorySize = "15KB" },
                PreserveLogFileNameExtension = false,
                MaximumFileSize = "10KB"
            });

            for (var i = 0; i < 1000; i++)
            {
                StringBuilder s = new StringBuilder();
                for (var j = 50; j < 100; j++)
                {
                    if (j > 50)
                    {
                        s.Append(" ");
                    }
                    s.Append(j);
                }
                _logger.Log(Level.Debug, s.ToString(), null);
            }

            appender.Cleaner.PeriodMinutes = 0;
            _logger.Log(Level.Debug, "just one more time", null);

            VerifyFileCount(2);
        }

        [Test]
        public void Skips_Locked_Files()
        {
            const string path1 = "sub\\a.txt";
            const string path2 = "sub\\sub\\b.txt";
            const string path3 = "sub\\other\\c.txt";
            CreateEmptyFile(path1, DateTime.UtcNow.AddDays(-3));
            CreateEmptyFile(path2, DateTime.UtcNow.AddDays(-3));
            CreateEmptyFile(path3, DateTime.UtcNow.AddDays(-3));
            var stream = LockFile(path2);

            SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Date,
                Cleaner = new SelfCleaner { MaximumFileAgeDays = 1.5 },
                DatePattern = "dd_MM_yyyy'.txt'"
            });

            _logger.Log(Level.Debug, "wat?", null);

            VerifyFileDoesNotExist(path1);
            VerifyFileDoesNotExist(path3);
            VerifyFileExists(path2);
            VerifyFileExists(CleaningCheckPath);
            VerifyFileCount(3);

            stream.Close();
        }

        [Test]
        public void Does_Not_Clean_If_Not_Due_For_Cleaning_Check()
        {
            var now = DateTime.UtcNow;
            var lastCheckTime = now.AddMilliseconds(-1);
            CreateEmptyFile(CleaningCheckPath, lastCheckTime);

            const string path1 = "sub\\a.txt";
            CreateEmptyFile(path1, DateTime.UtcNow.AddDays(-3));

            SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Date,
                Cleaner = new SelfCleaner { MaximumFileAgeDays = 1.5, PeriodMinutes = 20 },
                DatePattern = "dd_MM_yyyy'.txt'"
            });

            _logger.Log(Level.Debug, "just did the cleaning...", null);

            VerifyFileCount(3);
            VerifyFileExists(path1);
            var checkTime = File.GetLastWriteTimeUtc(CleaningCheckPath);
            Assert.That(checkTime, Is.EqualTo(lastCheckTime).Within(10).Milliseconds);
        }

        [Test]
        public void Cleans_After_Period_Expires()
        {
            var now = DateTime.UtcNow;
            const double minutesAgo = 20.0;
            const double period = minutesAgo + 0.03;
            var lastCheckTime = now.AddMinutes(-minutesAgo);
            var dueTime = lastCheckTime.AddMinutes(period);
            CreateEmptyFile(CleaningCheckPath, lastCheckTime);

            const string path1 = "sub\\a.txt";
            CreateEmptyFile(path1, DateTime.UtcNow.AddDays(-3));

            SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Date,
                Cleaner = new SelfCleaner { MaximumFileAgeDays = 1.5, PeriodMinutes = period },
                DatePattern = "dd_MM_yyyy'.txt'"
            });

            _logger.Log(Level.Debug, "just did the cleaning...", null);

            VerifyFileCount(3);
            VerifyFileExists(path1);
            var checkTime = File.GetLastWriteTimeUtc(CleaningCheckPath);
            Assert.That(checkTime, Is.EqualTo(lastCheckTime).Within(10).Milliseconds);

            var secondsWait = (dueTime - DateTime.UtcNow).TotalSeconds + 0.3;
            Task.Delay(TimeSpan.FromSeconds(secondsWait)).Wait();

            _logger.Log(Level.Debug, "ready to check again.", null);

            VerifyFileCount(2);
            VerifyFileDoesNotExist(path1);
            var checkTime2 = File.GetLastWriteTimeUtc(CleaningCheckPath);
            Assert.That(checkTime2, Is.EqualTo(DateTime.UtcNow).Within(1).Seconds);
        }

        [Test]
        public void Cleans_Old_Files_Until_Under_Size_Limit()
        {
            var now = DateTime.UtcNow;

            const string path1 = "sub\\a.txt";
            const string path2 = "sub\\a\\aa\\a.txt";
            const string path3 = "3.txt";
            CreateFile(path1, now.AddDays(-3), RandomGenerator.String(20000));
            CreateFile(path2, now.AddDays(-1.5), RandomGenerator.String(3000));
            CreateFile(path3, now.AddDays(-1.2), RandomGenerator.String(10000));

            SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Date,
                Cleaner = new SelfCleaner { MaximumDirectorySize = "12KB" },
                DatePattern = "dd_MM_yyyy'.txt'"
            });

            _logger.Log(Level.Debug, "getting rid of the big old files", null);

            VerifyFileCount(3);
            VerifyFileDoesNotExist(path1);
            VerifyFileDoesNotExist(path2);
            VerifyFileExists(path3);
            VerifyFileExists(CleaningCheckPath);
        }

        [TestCase("txt")]
        [TestCase(".txt")]
        public void Ignores_Files_With_Wrong_Extension(string extension)
        {
            var now = DateTime.UtcNow;

            const string path1 = "sub\\a.log";
            const string path2 = "sub\\a.txt.14";
            CreateFile(path1, now.AddDays(-3), RandomGenerator.String(20000));
            CreateFile(path2, now.AddDays(-3), RandomGenerator.String(20000));

            SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Date,
                Cleaner = new SelfCleaner { MaximumFileAgeDays = 0.5, FileExtension = extension },
                DatePattern = "dd_MM_yyyy'.txt'"
            });

            _logger.Log(Level.Debug, "ignoring files with wrong extension", null);

            VerifyFileCount(3);
            VerifyFileExists(path1);
            VerifyFileDoesNotExist(path2);
            VerifyFileExists(CleaningCheckPath);
        }

        private void CreateEmptyFile(string name, DateTime lastModified)
        {
            CreateFile(name, lastModified, "");
        }

        private FileStream LockFile(string path)
        {
            return new FileStream(LogPath(path), FileMode.Open, FileAccess.Read, FileShare.None);
        }

        private void CreateFile(string name, DateTime lastModified, string content)
        {
            var path = LogPath(name);
            var parent = Path.GetDirectoryName(path);
            if (parent != null && !Directory.Exists(parent)) Directory.CreateDirectory(parent);

            File.WriteAllText(path, content);
            File.SetLastWriteTimeUtc(path, lastModified);
        }

        private void VerifyFileExists(string path)
        {
            Assert.That(File.Exists(LogPath(path)), $"file not found: {path}");
        }

        private void VerifyFileDoesNotExist(string path)
        {
            Assert.False(File.Exists(LogPath(path)), $"file found: {path}");
        }

        private void VerifyDirectoryExists(string path)
        {
            Assert.That(Directory.Exists(LogPath(path)), $"directory not found: {path}");
        }

        private void VerifyDirectoryDoesNotExist(string path)
        {
            Assert.False(Directory.Exists(LogPath(path)), $"directory found: {path}");
        }
        private void VerifyFileCount(int expectedCount)
        {
            var count = GetFileCount(_testLogPath);
            Assert.That(count, Is.EqualTo(expectedCount));
        }

        private static int GetFileCount(string path)
        {
            return Directory.GetDirectories(path).Sum(GetFileCount) + Directory.GetFiles(path).Length;
        }

        private void DeleteFiles()
        {
            if (Directory.Exists(_testLogPath)) Directory.Delete(_testLogPath, true);
        }

        private string CleaningCheckPath => LogPath("lastcleaning.check");

        private string LogPath(string path) => Path.Combine(_testLogPath, path);

        private static void ResetLogging()
        {
            var repo = LogManager.GetRepository();
            repo.ResetConfiguration();
            repo.Shutdown();
            ((Hierarchy)repo).Clear();
        }
    }
}
