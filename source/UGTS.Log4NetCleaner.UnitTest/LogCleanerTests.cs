using System;
using System.Threading.Tasks;
using log4net.Appender;
using Moq;
using NUnit.Framework;
using UGTS.Log4NetCleaner.Interfaces;
using UGTS.Testing;

namespace UGTS.Log4NetCleaner.UnitTest
{
    [TestFixture]
    internal class LogCleanerTests : TestBase<LogCleaner, ILogCleaner>
    {
        protected override LogCleaner CreateTestObject()
        {
            var testObject = new LogCleaner
            {
                DirectoryCleaner = DefineMock<IDirectoryCleaner>().Object,
                TaskRunner = DefineMock<ITaskRunner>().Object
            };

            testObject.SetPrivateFieldValue("_dateTimeProvider", DefineMock<RollingFileAppender.IDateTime>().Object);
            testObject.SetPrivateFieldValue("_self", DefineMock<ILogCleaner>().Object); // ugly hack to test calling other methods on the same object
            return testObject;
        }

        internal class MaximumFileAgeDays : LogCleanerTests
        {
            [TestCase("1", 1.0)]
            [TestCase("89.33", 89.33)]
            [TestCase("not", null)]
            public void Reads_Sizes_Correctly(string value, double? expected)
            {
                TestObject.MaximumFileAgeDays = value;

                Assert.That(TestObject.MaxFileAgeDays, Is.EqualTo(expected));
            }
        }

        internal class MaximumDirectorySize : LogCleanerTests
        {
            [TestCase("1", 1L)]
            [TestCase("1KB", 1024L)]
            [TestCase("5KB", 5120L)]
            [TestCase("10MB", 10485760L)]
            [TestCase("15MB", 15728640L)]
            [TestCase("4GB", 4294967296L)]
            [TestCase("not", null)]
            public void Reads_Sizes_Correctly(string value, long? expected)
            {
                TestObject.MaximumDirectorySize = value;

                Assert.That(TestObject.MaxDirectorySize, Is.EqualTo(expected));
            }
        }

        internal class TryCleanup : LogCleanerTests
        {
            [Test, Combinatorial]
            public void Cleans_If_IsDue_And_Either_MaxAgeDays_Or_MaxSizeBytes_Specified(
                [Values(true, false)] bool hasMaxAge,
                [Values(true, false)] bool hasMaxBytes,
                [Values(null, "", " ", "a")] string fileExtension,
                [Values(true, false)] bool isDue)
            {
                var basePath = RandomGenerator.String();
                var lastRun = RandomGenerator.DateTime();
                var updatedTime = RandomGenerator.DateTime();
                if (hasMaxAge) TestObject.MaxFileAgeDays = 1;
                if (hasMaxBytes) TestObject.MaxDirectorySize = 1;
                TestObject.LastCleaning = lastRun;
                TestObject.BasePath = basePath;
                TestObject.FileExtension = fileExtension;
                Mock<IDirectoryCleaner>().Setup(x => x.UpdateLastCleaningTime(basePath)).Returns(updatedTime);
                Mock<ILogCleaner>().Setup(x => x.IsDueForCleaning(It.IsAny<DateTime>()))
                    .Returns(isDue);

                TestInterface.TryCleanup();

                if ((hasMaxAge || hasMaxBytes) && isDue && !string.IsNullOrWhiteSpace(fileExtension))
                {
                    Mock<ILogCleaner>().Verify(x => x.Cleanup(), Times.Once);
                    Assert.That(TestObject.LastCleaning, Is.EqualTo(updatedTime));
                }
                else
                {
                    Mock<ILogCleaner>().Verify(x => x.Cleanup(), Times.Never);
                    Assert.That(TestObject.LastCleaning, Is.EqualTo(lastRun));
                }
            }
        }

        internal class IsDueForCleaning : LogCleanerTests
        {
            [TestCase(-1, true)]
            [TestCase(1, false)]
            public void Checks_Cleaner_For_Last_Date_If_Never_Checked_Before(int secondsDelta, bool expectedResult)
            {
                var now = RandomGenerator.DateTime(new DateTime(2000, 1, 1));
                var basePath = RandomGenerator.String();
                var period = RandomGenerator.Double(30.0, 60.0);
                var updatedTime = now.AddMinutes(-period).AddSeconds(secondsDelta);
                TestObject.PeriodMinutes = period;
                TestObject.LastCleaning = null;
                TestObject.BasePath = basePath;
                Mock<IDirectoryCleaner>().Setup(x => x.GetLastCleaningTime(basePath)).Returns(updatedTime);

                var actual = TestInterface.IsDueForCleaning(now);

                Mock<IDirectoryCleaner>().Verify(x => x.GetLastCleaningTime(It.IsAny<string>()), Times.Once);
                Assert.That(TestObject.LastCleaning, Is.EqualTo(updatedTime));
                Assert.That(actual, Is.EqualTo(expectedResult));
            }

            [TestCase(-1, true)]
            [TestCase(1, false)]
            public void True_If_Last_Cleaned_At_Least_Period_Minutes_Ago(int secondsDelta, bool expectedResult)
            {
                var now = RandomGenerator.DateTime();
                var period = RandomGenerator.Double(30.0, 60.0);
                TestObject.PeriodMinutes = period;
                TestObject.LastCleaning = now.AddMinutes(-period).AddSeconds(secondsDelta);

                var actual = TestInterface.IsDueForCleaning(now);

                Assert.That(actual, Is.EqualTo(expectedResult));
            }
        }

        internal class Cleanup : LogCleanerTests
        {
            private Action _action;
            private Task _task;

            [SetUp]
            public void Setup()
            {
                _task = new Task(() => { });
                Mock<ITaskRunner>().Setup(x => x.Run(It.IsAny<Action>(), It.IsAny<WaitType>())).Returns(_task)
                    .Callback<Action, WaitType>((action, _) => _action = action);
            }

            [TestCase(WaitType.Never)]
            [TestCase(WaitType.Always)]
            public void Runs_Task_To_Clean_And_Returns_Task(WaitType waitType)
            {
                TestObject.WaitType = waitType;

                var actual = TestInterface.Cleanup();

                Assert.That(actual, Is.EqualTo(_task));
                Mock<ITaskRunner>().Verify(x => x.Run(It.IsAny<Action>(), waitType), Times.Once);
                Mock<ITaskRunner>().Verify(x => x.Run(It.IsAny<Action>(), It.IsAny<WaitType>()), Times.Once);
                Mock<IDirectoryCleaner>().Verify(x => x.Clean(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<long?>()), Times.Never);

                _action();

                Mock<IDirectoryCleaner>().Verify(x => x.Clean(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<long?>()), Times.Once);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void Cleans_With_Correct_Parameters(bool hasMaxAge)
            {
                var now = RandomGenerator.DateTime(new DateTime(2000, 1, 1), new DateTime(2010, 1, 1));
                var maxAge = RandomGenerator.Double(3.0, 8.0);
                var baseFile = RandomGenerator.String();
                var maxBytes = RandomGenerator.Long(1, 1000);
                var extension = RandomGenerator.String();
                TestObject.BasePath = baseFile;
                TestObject.FileExtension = extension;
                if (hasMaxAge) TestObject.MaxFileAgeDays = maxAge;
                TestObject.MaxDirectorySize = maxBytes;
                var expectedCutoff = hasMaxAge ? (DateTime?)now.AddDays(-maxAge) : null;
                Mock<RollingFileAppender.IDateTime>().Setup(x => x.Now).Returns(now);

                TestInterface.Cleanup();
                _action();

                Mock<IDirectoryCleaner>().Verify(x => x.Clean(baseFile, extension, expectedCutoff, maxBytes), Times.Once);
                Mock<IDirectoryCleaner>().Verify(x => x.Clean(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<long?>()), Times.Once);
            }
        }
    }
}