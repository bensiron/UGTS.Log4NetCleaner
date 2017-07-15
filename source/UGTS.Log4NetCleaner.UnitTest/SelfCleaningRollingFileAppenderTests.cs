using log4net.Core;
using Moq;
using NUnit.Framework;
using UGTS.Log4NetCleaner.Interfaces;
using UGTS.Testing;

namespace UGTS.Log4NetCleaner.UnitTest
{
    [TestFixture]
    internal class SelfCleaningRollingFileAppenderTests : TestBase<SelfCleaningRollingFileAppender, ISelfCleaningRollingFileAppender>
    {
        protected override SelfCleaningRollingFileAppender CreateTestObject()
        {
            var testObject = new SelfCleaningRollingFileAppender
            {
                Cleaner = DefineMock<ILogCleaner>().Object,
            };

            testObject.SetPrivateFieldValue("_self", DefineMock<ISelfCleaningRollingFileAppender>().Object); // ugly hack to test calling other methods on the same object
            return testObject;
        }

        internal class ActivateOptions : SelfCleaningRollingFileAppenderTests
        {
            [Test]
            public void Sets_BasePath_Before_Calling_Base_Method()
            {
                var file = RandomGenerator.String();
                TestObject.File = file;
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.ActivateOptionsBase())
                    .Callback(() => Mock<ILogCleaner>().VerifySet(x => x.BasePath = file));

                TestObject.ActivateOptions();

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.ActivateOptionsBase(), Times.Once);
            }

            [Test]
            public void Does_Not_Set_BaseFile_If_Already_Set()
            {
                TestObject.File = RandomGenerator.String();
                Mock<ILogCleaner>().Setup(x => x.BasePath).Returns(RandomGenerator.String());

                TestObject.ActivateOptions();

                Mock<ILogCleaner>().VerifySet(x => x.BasePath = It.IsAny<string>(), Times.Never);
            }

            [Test]
            public void Sets_File_Extension_After_Calling_Base_Method()
            {
                var file = RandomGenerator.String();
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.ActivateOptionsBase())
                    .Callback(() =>
                    {
                        TestObject.File = file;
                        Mock<ILogCleaner>().Verify(x => x.InferFileExtension(It.IsAny<string>()), Times.Never);
                    });

                TestObject.ActivateOptions();

                Mock<ILogCleaner>().Verify(x => x.InferFileExtension(file));
                Mock<ILogCleaner>().Verify(x => x.InferFileExtension(It.IsAny<string>()), Times.Once);
            }

            [Test]
            public void Does_Not_InferFileExtension_If_Already_Set()
            {
                Mock<ILogCleaner>().Setup(x => x.FileExtension).Returns(RandomGenerator.String());

                TestObject.ActivateOptions();

                Mock<ILogCleaner>().Verify(x => x.InferFileExtension(It.IsAny<string>()), Times.Never);
            }
        }

        internal class Append : SelfCleaningRollingFileAppenderTests
        {
            [Test]
            public void Cleans_Before_Appending()
            {
                var logEvent = new LoggingEvent(new LoggingEventData());
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.AppendBase(It.IsAny<LoggingEvent>()))
                    .Callback(() => Mock<ILogCleaner>().Verify(x => x.TryCleanup(), Times.Once));

                TestObject.Append(logEvent);

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(logEvent));
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(It.IsAny<LoggingEvent>()), Times.Once);
                Mock<ILogCleaner>().Verify(x => x.TryCleanup(), Times.Once);
            }
        }

        internal class AppendMultiple : SelfCleaningRollingFileAppenderTests
        {
            [Test]
            public void Cleans_Before_Appending()
            {
                var logEvents = new LoggingEvent[0];
                Mock<ISelfCleaningRollingFileAppender>().Setup(x => x.AppendBase(It.IsAny<LoggingEvent>()))
                    .Callback(() => Mock<ILogCleaner>().Verify(x => x.TryCleanup(), Times.Once));

                TestObject.Append(logEvents);

                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(logEvents));
                Mock<ISelfCleaningRollingFileAppender>().Verify(x => x.AppendBase(It.IsAny<LoggingEvent[]>()), Times.Once);
                Mock<ILogCleaner>().Verify(x => x.TryCleanup(), Times.Once);
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
