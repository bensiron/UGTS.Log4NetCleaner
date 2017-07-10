﻿using System;
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
                DateTimeProvider = DefineMock<RollingFileAppender.IDateTime>().Object,
                TaskRunner = DefineMock<ITaskRunner>().Object
            };

            testObject.SetPrivateFieldValue("_self", DefineMock<ISelfCleaningRollingFileAppender>().Object); // ugly hack to test calling other methods on the same object
            return testObject;
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
                if (hasMaxAge) TestObject.CleaningMaxAgeDays = 1;
                if (hasMaxBytes) TestObject.CleaningMaxSizeBytes = 1;
                TestObject.LastCleaning = lastRun;
                TestObject.CleaningBasePath = basePath;
                Mock<IDirectoryCleaner>().Setup(x => x.UpdateLastCleaningTime(basePath)).Returns(updatedTime);
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.IsDueForCleaning(It.IsAny<DateTime>()))
                    .Returns(isDue);

                TestInterface.TryCleanupLogDirectory();

                if ((hasMaxAge || hasMaxBytes) && isDue)
                {
                    Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.CleanupLogDirectory(), Times.Once);
                    Assert.That(TestObject.LastCleaning, Is.EqualTo(updatedTime));
                }
                else
                {
                    Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.CleanupLogDirectory(), Times.Never);
                    Assert.That(TestObject.LastCleaning, Is.EqualTo(lastRun));
                }
            }

            [TestCase(true)]
            [TestCase(false)]
            public void Passes_Correct_Parameters_To_Should_Wait(bool firstTime)
            {
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.CleanupLogDirectory()).Returns(Task.Run(() => {}));
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.IsDueForCleaning(It.IsAny<DateTime>())).Returns(true);
                TestObject.CleaningMaxAgeDays = 1;
                TestObject.LastCleaning = firstTime ? (DateTime?) null : RandomGenerator.DateTime();

                TestObject.TryCleanupLogDirectory();

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.ShouldWaitForCleaning(firstTime));
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.ShouldWaitForCleaning(It.IsAny<bool>()), Times.Once);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void Waits_For_Cleaning_Task_If_Should_Wait(bool shouldWait)
            {
                Task task = null;
                Func<Task> taskStarter = () =>
                {
                    task = Task.Delay(100);
                    return task;
                };

                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.CleanupLogDirectory()).Returns(taskStarter);
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.IsDueForCleaning(It.IsAny<DateTime>())).Returns(true);
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.ShouldWaitForCleaning(It.IsAny<bool>())).Returns(shouldWait);
                TestObject.CleaningMaxAgeDays = 1;
                
                TestObject.TryCleanupLogDirectory();

                Assert.That(task.IsCompleted, Is.EqualTo(shouldWait));

                task.Wait();
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
                [Values(true, false)] bool wasFirstTime,
                [Values(CleaningWaitType.Never, CleaningWaitType.FirstTimeOnly, CleaningWaitType.Always)] CleaningWaitType waitType)
            {
                TestObject.CleaningWaitType = waitType;
                var expected = waitType == CleaningWaitType.Always || waitType == CleaningWaitType.FirstTimeOnly && wasFirstTime;

                var actual = TestInterface.ShouldWaitForCleaning(wasFirstTime);

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
                Mock<ITaskRunner>().Setup(x => x.Run(It.IsAny<Action>())).Returns(_task)
                    .Callback<Action>(action => _action = action);
            }

            [Test]
            public void Runs_Task_To_Clean_And_Returns_Task()
            {
                var actual = TestInterface.CleanupLogDirectory();

                Assert.That(actual, Is.EqualTo(_task));
                Mock<ITaskRunner>().Verify(x => x.Run(It.IsAny<Action>()), Times.Once);
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
                var maxBytes = RandomGenerator.Long();
                var extension = RandomGenerator.String();
                TestObject.CleaningBasePath = baseFile;
                TestObject.CleaningFileExtension = extension;
                if (hasMaxAge) TestObject.CleaningMaxAgeDays = maxAge;
                TestObject.CleaningMaxSizeBytes = maxBytes;
                var expectedCutoff = hasMaxAge ? (DateTime?)now.AddDays(-maxAge) : null;
                Mock<RollingFileAppender.IDateTime>().Setup(x => x.Now).Returns(now);

                TestInterface.CleanupLogDirectory();
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
