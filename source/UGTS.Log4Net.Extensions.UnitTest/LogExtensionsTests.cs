﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using log4net;
using log4net.Core;
using log4net.Repository;
using Moq;
using NUnit.Framework;
using UGTS.Testing;

namespace UGTS.Log4Net.Extensions.UnitTest
{
    [TestFixture]
    public class LogExtensionsTests
    {
        private Mock<ILog> _log;
        private Mock<ILogger> _logger;

        [SetUp]
        public void Setup()
        {
            _log = new Mock<ILog>();
            _logger = new Mock<ILogger>();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Fatal(bool enabled)
        {
            VerifyLogging(x => x.IsFatalEnabled, Level.Fatal, enabled,
                message => _log.Object.Fatal(() => message));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Error(bool enabled)
        {
            VerifyLogging(x => x.IsErrorEnabled, Level.Error, enabled,
                message => _log.Object.Error(() => message));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Warn(bool enabled)
        {
            VerifyLogging(x => x.IsWarnEnabled, Level.Warn, enabled,
                message => _log.Object.Warn(() => message));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Info(bool enabled)
        {
            VerifyLogging(x => x.IsInfoEnabled, Level.Info, enabled,
                message => _log.Object.Info(() => message));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Debug(bool enabled)
        {
            VerifyLogging(x => x.IsDebugEnabled, Level.Debug, enabled,
                message => _log.Object.Debug(() => message));
        }

        [Test, Combinatorial]
        public void WithLevel([Values(true, false)] bool enabled, [ValueSource(nameof(LevelSource))] Level level)
        {
            var expectedMessage = RandomGenerator.String();
            var expectedTimes = enabled ? 1 : 0;
            _logger.Setup(x => x.IsEnabledFor(level)).Returns(enabled);
            var expectedLevel = SetupGetLevel(level);

            _log.Object.WithLevel(level, expectedMessage);

            VerifyLogged(expectedTimes, expectedLevel, expectedMessage);
        }

        [UsedImplicitly]
        private static IEnumerable<Level> LevelSource()
        {
            return new [] {Level.Debug, Level.Info, Level.Warn, Level.Error, Level.Fatal};
        }

        private void VerifyLogging(Expression<Func<ILog, bool>> mockExpression, Level level, bool enabled, Action<string> testAction)
        {
            var expectedMessage = RandomGenerator.String();
            var expectedTimes = enabled ? 1 : 0;
            _log.Setup(mockExpression).Returns(enabled);
            var expectedLevel = SetupGetLevel(level);

            testAction(expectedMessage);

            VerifyLogged(expectedTimes, expectedLevel, expectedMessage);
        }

        private Level SetupGetLevel(Level input)
        {
            var repo = new Mock<ILoggerRepository>();
            var map = new LevelMap();
            var output = new Level(input.Value, input.Name);
            map.Add(output);
            _log.Setup(x => x.Logger).Returns(_logger.Object);
            _logger.Setup(x => x.Repository).Returns(repo.Object);
            repo.Setup(x => x.LevelMap).Returns(map);
            return output;
        }

        private void VerifyLogged(int expectedTimes, Level expectedLevel, string expectedMessage)
        {
            if (expectedTimes > 0) _logger.Verify(x => x.Log(typeof(LogExtensions), expectedLevel, expectedMessage, null));
            _logger.Verify(x => x.Log(It.IsAny<Type>(), It.IsAny<Level>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Exactly(expectedTimes));
        }
    }
}