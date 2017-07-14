using System;
using JetBrains.Annotations;
using log4net;
using log4net.Core;

namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// ILog extensions methods
    /// </summary>
    public static class LogExtensions
    {
        /// <summary>
        /// Logs a message at the specified level chosen at runtime.
        /// </summary>
        [UsedImplicitly]
        public static void WithLevel(this ILog log, Level level, string message)
        {
            if (!log.Logger.IsEnabledFor(level)) return;
            Log(log, level, message);
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Takes care to preserve any call stack information output in the logs.
        /// </summary>
        [UsedImplicitly]
        public static void Fatal(this ILog log, Func<string> generator)
        {
            if (!log.IsFatalEnabled) return;
            Log(log, Level.Fatal, generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Takes care to preserve any call stack information output in the logs.
        /// </summary>
        [UsedImplicitly]
        public static void Error(this ILog log, Func<string> generator)
        {
            if (!log.IsErrorEnabled) return;
            Log(log, Level.Error, generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Takes care to preserve any call stack information output in the logs.
        /// </summary>
        [UsedImplicitly]
        public static void Warn(this ILog log, Func<string> generator)
        {
            if (!log.IsWarnEnabled) return;
            Log(log, Level.Warn, generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Takes care to preserve any call stack information output in the logs.
        /// </summary>
        [UsedImplicitly]
        public static void Info(this ILog log, Func<string> generator)
        {
            if (!log.IsInfoEnabled) return;
            Log(log, Level.Info, generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Takes care to preserve any call stack information output in the logs.
        /// </summary>
        [UsedImplicitly]
        public static void Debug(this ILog log, Func<string> generator)
        {
            if (!log.IsDebugEnabled) return;
            Log(log, Level.Debug, generator());
        }

        private static void Log(ILoggerWrapper log, Level defaultLevel, string message)
        {
            var level = GetLevel(log, defaultLevel);
            log.Logger.Log(typeof(LogExtensions), level, message, null);
        }

        private static Level GetLevel(ILoggerWrapper log, Level defaultLevel)
        {
            return log.Logger.Repository.LevelMap.LookupWithDefault(defaultLevel);
        }
    }
}
