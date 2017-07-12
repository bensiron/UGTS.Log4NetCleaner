using System;
using System.Threading.Tasks;
using log4net.Appender;
using Moq;
using NUnit.Framework;
using UGTS.Log4Net.Extensions.Interfaces;
using UGTS.Testing;

namespace UGTS.Log4Net.Extensions.UnitTest
{
    [TestFixture]
    internal class SelfCleanerTests : TestBase<LogCleaner, ILogCleaner>
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

        internal class CleaningMaximumFileAgeDays : SelfCleanerTests
        {
            [TestCase(1, 1.0)]
            [TestCase(89.33, 89.33)]
            [TestCase(-2.3, 0.0)]
            [TestCase(1e7, 500000.0)]
            public void Reads_Sizes_Correctly(double value, double expected)
            {
                TestObject.MaximumFileAgeDays = value;

                Assert.That(TestObject.MaximumFileAgeDays, Is.EqualTo(expected));
            }
        }

        internal class CleaningMaximumDirectorySize : SelfCleanerTests
        {
            [TestCase("1", "1")]
            [TestCase("1KB", "1024")]
            [TestCase("5KB", "5120")]
            [TestCase("10MB", "10485760")]
            [TestCase("15MB", "15728640")]
            [TestCase("4GB", "4294967296")]
            [TestCase("not", "")]
            public void Reads_Sizes_Correctly(string value, string expected)
            {
                TestObject.MaximumDirectorySize = value;

                Assert.That(TestObject.MaximumDirectorySize, Is.EqualTo(expected));
            }
        }

        internal class TryCleanup : SelfCleanerTests
        {
            [Test, Combinatorial]
            public void Cleans_If_IsDue_And_Either_MaxAgeDays_Or_MaxSizeBytes_Specified(
                [Values(true, false)] bool hasMaxAge,
                [Values(true, false)] bool hasMaxBytes,
                [Values(true, false)] bool isDue)
            {
                var basePath = RandomGenerator.String();
                var lastRun = RandomGenerator.DateTime();
                var updatedTime = RandomGenerator.DateTime();
                if (hasMaxAge) TestObject.MaximumFileAgeDays = 1;
                if (hasMaxBytes) TestObject.MaximumDirectorySize = "1";
                TestObject.LastCleaning = lastRun;
                TestObject.BasePath = basePath;
                Mock<IDirectoryCleaner>().Setup(x => x.UpdateLastCleaningTime(basePath)).Returns(updatedTime);
                Mock<ILogCleaner>().Setup(x => x.IsDueForCleaning(It.IsAny<DateTime>()))
                    .Returns(isDue);

                TestInterface.TryCleanup();

                if ((hasMaxAge || hasMaxBytes) && isDue)
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

        internal class IsDueForCleaning : SelfCleanerTests
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

        internal class Cleanup : SelfCleanerTests
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
                if (hasMaxAge) TestObject.MaximumFileAgeDays = maxAge;
                TestObject.MaximumDirectorySize = maxBytes.ToString();
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