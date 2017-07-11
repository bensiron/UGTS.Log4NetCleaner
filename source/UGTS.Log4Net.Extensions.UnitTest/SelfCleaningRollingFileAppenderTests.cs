using System;
using System.Globalization;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using Moq;
using NUnit.Framework;
using UGTS.Log4Net.Extensions.Interfaces;
using UGTS.Testing;

namespace UGTS.Log4Net.Extensions.UnitTest
{
    [TestFixture]
    internal class SelfCleaningRollingFileAppenderTests : TestBase<SelfCleaningRollingFileAppender, ISelfCleaningRollingFileAppender>
    {
        protected override SelfCleaningRollingFileAppender CreateTestObject()
        {
            var testObject = new SelfCleaningRollingFileAppender
            {
                Cleaner = DefineMock<IDirectoryCleaner>().Object,
                CleaningTaskRunner = DefineMock<ITaskRunner>().Object
            };

            testObject.SetPrivateFieldValue("_dateTimeProvider", DefineMock<RollingFileAppender.IDateTime>().Object);
            testObject.SetPrivateFieldValue("_self", DefineMock<ISelfCleaningRollingFileAppender>().Object); // ugly hack to test calling other methods on the same object
            return testObject;
        }

        internal class CleaningMaximumFileAgeDays : SelfCleaningRollingFileAppenderTests
        {
            [TestCase("1", "1")]
            [TestCase("89.33", "89.33")]
            [TestCase("-2.3", "-2.3")]
            [TestCase("not", "")]
            public void Reads_Sizes_Correctly(string value, string expected)
            {
                TestObject.CleaningMaximumFileAgeDays = value;

                Assert.That(TestObject.CleaningMaximumFileAgeDays, Is.EqualTo(expected));
            }
        }

        internal class CleaningMaximumDirectorySize : SelfCleaningRollingFileAppenderTests
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
                TestObject.CleaningMaximumDirectorySize = value;

                Assert.That(TestObject.CleaningMaximumDirectorySize, Is.EqualTo(expected));
            }               
        }

        internal class ActivateOptions : SelfCleaningRollingFileAppenderTests
        {
            [Test]
            public void Sets_BaseFile_Before_Calling_Base_Method()
            {
                var file = RandomGenerator.String();
                TestObject.File = file;
                TestObject.CleaningBasePath = null;
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.ActivateOptionsBase())
                    .Callback(() => Assert.That(TestObject.CleaningBasePath, Is.EqualTo(file)));

                TestObject.ActivateOptions();

                Assert.That(TestObject.CleaningBasePath, Is.EqualTo(file));
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.ActivateOptionsBase(), Times.Once);
            }

            [TestCase("a")]
            [TestCase(null)]
            public void Does_Not_Set_BaseFile_If_Already_Set(string file)
            {
                var baseFile = RandomGenerator.String();
                TestObject.File = file;
                TestObject.CleaningBasePath = baseFile;

                TestObject.ActivateOptions();

                Assert.That(TestObject.CleaningBasePath, Is.EqualTo(baseFile));
            }

            [Test]
            public void Sets_File_Extension_After_Calling_Base_Method()
            {
                var file = RandomGenerator.String();
                var extension = RandomGenerator.String();
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.ActivateOptionsBase())
                    .Callback(() => TestObject.File = file);
                Mock<IDirectoryCleaner>().Setup(x => x.GetFileExtension(file)).Returns(extension);

                TestObject.ActivateOptions();

                Mock<IDirectoryCleaner>().Verify(x => x.GetFileExtension(It.IsAny<string>()), Times.Once);
                Assert.That(TestObject.CleaningFileExtension, Is.EqualTo(extension));
            }

            [Test]
            public void Does_Not_Set_Extension_If_Already_Set()
            {
                var extension = RandomGenerator.String();
                TestObject.CleaningFileExtension = extension;

                TestObject.ActivateOptions();

                Assert.That(TestObject.CleaningFileExtension, Is.EqualTo(extension));
                Mock<IDirectoryCleaner>().Verify(x => x.GetFileExtension(It.IsAny<string>()), Times.Never);
            }
        }

        internal class Append : SelfCleaningRollingFileAppenderTests
        {
            [Test]
            public void Cleans_Before_Appending()
            {
                var logEvent = new LoggingEvent(new LoggingEventData());
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.AppendBase(It.IsAny<LoggingEvent>()))
                    .Callback(
                        () => Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.TryCleanupLogDirectory(), Times.Once));

                TestObject.Append(logEvent);

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(logEvent));
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(It.IsAny<LoggingEvent>()), Times.Once);
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.TryCleanupLogDirectory(), Times.Once);
            }
        }

        internal class AppendMultiple : SelfCleaningRollingFileAppenderTests
        {
            [Test]
            public void Cleans_Before_Appending()
            {
                var logEvents = new LoggingEvent[0];
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.AppendBase(It.IsAny<LoggingEvent>()))
                    .Callback(() => Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.TryCleanupLogDirectory(), Times.Once));

                TestObject.Append(logEvents);

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(logEvents));
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(It.IsAny<LoggingEvent[]>()), Times.Once);
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.TryCleanupLogDirectory(), Times.Once);
            }
        }

        internal class TryCleanupLogDirectory : SelfCleaningRollingFileAppenderTests
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
                if (hasMaxAge) TestObject.CleaningMaximumFileAgeDays = "1";
                if (hasMaxBytes) TestObject.CleaningMaximumDirectorySize = "1";
                TestObject.LastCleaning = lastRun;
                TestObject.CleaningBasePath = basePath;
                Mock<IDirectoryCleaner>().Setup(x => x.UpdateLastCleaningTime(basePath)).Returns(updatedTime);
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.IsDueForCleaning(It.IsAny<DateTime>()))
                    .Returns(isDue);

                TestInterface.TryCleanupLogDirectory();

                if ((hasMaxAge || hasMaxBytes) && isDue)
                {
                    Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.CleanupLogDirectory(It.IsAny<bool>()), Times.Once);
                    Assert.That(TestObject.LastCleaning, Is.EqualTo(updatedTime));
                }
                else
                {
                    Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.CleanupLogDirectory(It.IsAny<bool>()), Times.Never);
                    Assert.That(TestObject.LastCleaning, Is.EqualTo(lastRun));
                }
            }

            [TestCase(true)]
            [TestCase(false)]
            public void Checks_ShouldWait_Before_Setting_LastCleaning(bool firstTime)
            {
                var originalLastCleaning = firstTime ? (DateTime?) null : RandomGenerator.DateTime();
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.ShouldWaitForCleaning())
                    .Callback(() => Assert.That(TestObject.LastCleaning, Is.EqualTo(originalLastCleaning)));

                TestObject.CleaningMaximumFileAgeDays = "1";
                TestObject.LastCleaning = originalLastCleaning;

                TestObject.TryCleanupLogDirectory();

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.ShouldWaitForCleaning(), Times.Once);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void Passes_ShouldWait_To_CleanupLogDirectory(bool shouldWait)
            {
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.ShouldWaitForCleaning()).Returns(shouldWait);
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.IsDueForCleaning(It.IsAny<DateTime>())).Returns(true);
                TestObject.CleaningMaximumFileAgeDays = "1";
                
                TestObject.TryCleanupLogDirectory();

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.CleanupLogDirectory(shouldWait));
            }
        }

        internal class IsDueForCleaning : SelfCleaningRollingFileAppenderTests
        {
            [TestCase(-1, true)]
            [TestCase(1, false)]
            public void Checks_Cleaner_For_Last_Date_If_Never_Checked_Before(int secondsDelta, bool expectedResult)
            {
                var now = RandomGenerator.DateTime(new DateTime(2000, 1, 1));
                var basePath = RandomGenerator.String();
                var period = RandomGenerator.Double(30.0, 60.0);
                var updatedTime = now.AddMinutes(-period).AddSeconds(secondsDelta);
                TestObject.CleaningPeriodMinutes = period;
                TestObject.LastCleaning = null;
                TestObject.CleaningBasePath = basePath;
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
                TestObject.CleaningPeriodMinutes = period;
                TestObject.LastCleaning = now.AddMinutes(-period).AddSeconds(secondsDelta);

                var actual = TestInterface.IsDueForCleaning(now);

                Assert.That(actual, Is.EqualTo(expectedResult));
            }
        }

        internal class ShouldWaitForCleaning : SelfCleaningRollingFileAppenderTests
        {
            [Test, Combinatorial]
            public void RunTest(
                [Values(true, false)] bool firstTime,
                [Values(CleaningWaitType.Never, CleaningWaitType.FirstTimeOnly, CleaningWaitType.Always)] CleaningWaitType waitType)
            {
                TestObject.CleaningWaitType = waitType;
                TestObject.LastCleaning = firstTime ? (DateTime?) null : RandomGenerator.DateTime();
                var expected = waitType == CleaningWaitType.Always || waitType == CleaningWaitType.FirstTimeOnly & firstTime;

                var actual = TestInterface.ShouldWaitForCleaning();

                Assert.That(actual, Is.EqualTo(expected));                
            }
        }

        internal class CleanupLogDirectory: SelfCleaningRollingFileAppenderTests
        {
            private Action _action;
            private Task _task;

            [SetUp]
            public void Setup()
            {
                _task = new Task(() => {});
                Mock<ITaskRunner>().Setup(x => x.Run(It.IsAny<Action>(), It.IsAny<bool>())).Returns(_task)
                    .Callback<Action, bool>((action, _) => _action = action);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void Runs_Task_To_Clean_And_Returns_Task(bool shouldWait)
            {
                var actual = TestInterface.CleanupLogDirectory(shouldWait);

                Assert.That(actual, Is.EqualTo(_task));
                Mock<ITaskRunner>().Verify(x => x.Run(It.IsAny<Action>(), shouldWait), Times.Once);
                Mock<ITaskRunner>().Verify(x => x.Run(It.IsAny<Action>(), It.IsAny<bool>()), Times.Once);
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
                TestObject.CleaningBasePath = baseFile;
                TestObject.CleaningFileExtension = extension;
                if (hasMaxAge) TestObject.CleaningMaximumFileAgeDays = maxAge.ToString(CultureInfo.InvariantCulture);
                TestObject.CleaningMaximumDirectorySize = maxBytes.ToString();
                var expectedCutoff = hasMaxAge ? (DateTime?)now.AddDays(-maxAge) : null;
                Mock<RollingFileAppender.IDateTime>().Setup(x => x.Now).Returns(now);

                TestInterface.CleanupLogDirectory(false);
                _action();

                Mock<IDirectoryCleaner>().Verify(x => x.Clean(baseFile, extension, expectedCutoff, maxBytes), Times.Once);
                Mock<IDirectoryCleaner>().Verify(x => x.Clean(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<long?>()), Times.Once);
            }
        }
    }

    internal static class SelfCleaningRollingFileAppenderExtensions
    {
        public static void Append(this SelfCleaningRollingFileAppender appender, LoggingEvent logEvent)
        {
            appender.InvokePrivateMethod("Append", new [] {typeof(LoggingEvent)}, new object[] {logEvent});
        }

        public static void Append(this SelfCleaningRollingFileAppender appender, LoggingEvent[] events)
        {
            appender.InvokePrivateMethod("Append", new[] { typeof(LoggingEvent[]) }, new object[] { events });
        }
    }
}
