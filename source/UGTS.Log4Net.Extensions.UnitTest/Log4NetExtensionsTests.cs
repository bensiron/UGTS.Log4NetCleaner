using log4net;
using Moq;
using NUnit.Framework;
using UGTS.Testing;

namespace UGTS.Log4Net.Extensions.UnitTest
{
    [TestFixture]
    public class Log4NetExtensionsTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Fatal(bool enabled)
        {
            var log = new Mock<ILog>();
            var expected = RandomGenerator.String();
            var expectedTimes = enabled ? 1 : 0;
            log.Setup(x => x.IsFatalEnabled).Returns(enabled);

            log.Object.Fatal(() => expected);

            log.Verify(x => x.Fatal(expected), Times.Exactly(expectedTimes));
            log.Verify(x => x.Fatal(It.IsAny<string>()), Times.Exactly(expectedTimes));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Error(bool enabled)
        {
            var log = new Mock<ILog>();
            var expected = RandomGenerator.String();
            var expectedTimes = enabled ? 1 : 0;
            log.Setup(x => x.IsErrorEnabled).Returns(enabled);

            log.Object.Error(() => expected);

            log.Verify(x => x.Error(expected), Times.Exactly(expectedTimes));
            log.Verify(x => x.Error(It.IsAny<string>()), Times.Exactly(expectedTimes));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Warn(bool enabled)
        {
            var log = new Mock<ILog>();
            var expected = RandomGenerator.String();
            var expectedTimes = enabled ? 1 : 0;
            log.Setup(x => x.IsWarnEnabled).Returns(enabled);

            log.Object.Warn(() => expected);

            log.Verify(x => x.Warn(expected), Times.Exactly(expectedTimes));
            log.Verify(x => x.Warn(It.IsAny<string>()), Times.Exactly(expectedTimes));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Info(bool enabled)
        {
            var log = new Mock<ILog>();
            var expected = RandomGenerator.String();
            var expectedTimes = enabled ? 1 : 0;
            log.Setup(x => x.IsInfoEnabled).Returns(enabled);

            log.Object.Info(() => expected);

            log.Verify(x => x.Info(expected), Times.Exactly(expectedTimes));
            log.Verify(x => x.Info(It.IsAny<string>()), Times.Exactly(expectedTimes));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Debug(bool enabled)
        {
            var log = new Mock<ILog>();
            var expected = RandomGenerator.String();
            var expectedTimes = enabled ? 1 : 0;
            log.Setup(x => x.IsDebugEnabled).Returns(enabled);

            log.Object.Debug(() => expected);

            log.Verify(x => x.Debug(expected), Times.Exactly(expectedTimes));
            log.Verify(x => x.Debug(It.IsAny<string>()), Times.Exactly(expectedTimes));
        }
    }
}
