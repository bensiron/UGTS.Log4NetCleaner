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

        private void SetAppender(Func<SelfCleaningRollingFileAppender> generator)
        {
            var appender = generator();

            appender.Layout = _layout;
            appender.AppendToFile = true;
            appender.StaticLogFileName = false;
            appender.File = _testLogPath + "\\";

            appender.ActivateOptions();
            _logger.AddAppender(appender);
            _logger.Repository.Configured = true;
        }

        [Test]
        public void Can_Roll_By_File_Size()
        {
            SetAppender(() => new SelfCleaningRollingFileAppender
            {
                RollingStyle = RollingFileAppender.RollingMode.Composite,
                DatePattern = "dd_MM_yyyy",
                MaxSizeRollBackups = 1,
                CountDirection = 1,
                PreserveLogFileNameExtension = true,
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

            VerifyFileCount(2);
        }

        [Test]
        public void Creates_Cleaning_Check_File_At_First_Log_Call()
        {
            SetAppender(() => new SelfCleaningRollingFileAppender { MaxAgeDays = 1 });

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
                MaxAgeDays = 1.5,
                CleaningWaitType = CleaningWaitType.Always,
                DatePattern = "dd_MM_yyyy'.txt'",
                MaxSizeRollBackups = 1,
                PreserveLogFileNameExtension = true,
            });

            _logger.Log(Level.Debug, "wat?", null);

            VerifyFileCount(3);
            VerifyFileDoesNotExist(path1);
            VerifyDirectoryExists("sub");
            VerifyFileDoesNotExist(path2);
            VerifyDirectoryDoesNotExist("sub\\sub");
            VerifyFileExists(path3);
            VerifyDirectoryExists("sub\\other");
            VerifyFileExists(CleaningCheckPath);
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
                MaxAgeDays = 1.5,
                CleaningPeriodMinutes = 20,
                CleaningWaitType = CleaningWaitType.Always,
                DatePattern = "dd_MM_yyyy'.txt'",
                MaxSizeRollBackups = 1,
                PreserveLogFileNameExtension = true,
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
                MaxAgeDays = 1.5,
                CleaningPeriodMinutes = period,
                CleaningWaitType = CleaningWaitType.Always,
                DatePattern = "dd_MM_yyyy'.txt'",
                MaxSizeRollBackups = 1,
                PreserveLogFileNameExtension = true,
            });

            _logger.Log(Level.Debug, "just did the cleaning...", null);

            VerifyFileCount(3);
            VerifyFileExists(path1);
            var checkTime = File.GetLastWriteTimeUtc(CleaningCheckPath);
            Assert.That(checkTime, Is.EqualTo(lastCheckTime).Within(10).Milliseconds);

            var secondsWait = (dueTime - DateTime.UtcNow).TotalSeconds + 1.0;
            Task.Delay(TimeSpan.FromSeconds(secondsWait)).Wait();

            _logger.Log(Level.Debug, "ready to check again.", null);

            VerifyFileCount(2);
            VerifyFileDoesNotExist(path1);
            var checkTime2 = File.GetLastWriteTimeUtc(CleaningCheckPath);
            Assert.That(checkTime2, Is.EqualTo(DateTime.UtcNow).Within(1).Seconds);
        }

        private void CreateEmptyFile(string name, DateTime lastModified)
        {
            var path = Path.Combine(_testLogPath, name);
            var parent = Path.GetDirectoryName(path);
            if (parent != null && !Directory.Exists(parent)) Directory.CreateDirectory(parent);

            File.WriteAllBytes(path, new byte[0]);
            File.SetLastWriteTimeUtc(path, lastModified);
        }

        private void VerifyFileExists(string path)
        {
            Assert.That(File.Exists(Path.Combine(_testLogPath, path)), $"file not found: {path}");
        }

        private void VerifyFileDoesNotExist(string path)
        {
            Assert.False(File.Exists(Path.Combine(_testLogPath, path)), $"file found: {path}");
        }

        private void VerifyDirectoryExists(string path)
        {
            Assert.That(Directory.Exists(Path.Combine(_testLogPath, path)), $"directory not found: {path}");
        }

        private void VerifyDirectoryDoesNotExist(string path)
        {
            Assert.False(Directory.Exists(Path.Combine(_testLogPath, path)), $"directory found: {path}");
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

        private string CleaningCheckPath => Path.Combine(_testLogPath, "lastcleaning.check");

        private static void ResetLogging()
        {
            var repo = LogManager.GetRepository();
            repo.ResetConfiguration();
            repo.Shutdown();
            ((Hierarchy)repo).Clear();
        }
    }
}
