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
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Fatal(this ILog log, Func<string> generator)
        {
            if (!log.IsFatalEnabled) return;
            Log(log, Level.Fatal, generator);
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Error(this ILog log, Func<string> generator)
        {
            if (!log.IsErrorEnabled) return;
            Log(log, Level.Error, generator);
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Warn(this ILog log, Func<string> generator)
        {
            if (!log.IsWarnEnabled) return;
            Log(log, Level.Warn, generator);
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Info(this ILog log, Func<string> generator)
        {
            if (!log.IsInfoEnabled) return;
            Log(log, Level.Info, generator);
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Debug(this ILog log, Func<string> generator)
        {
            if (!log.IsDebugEnabled) return;
            Log(log, Level.Debug, generator);
        }

        private static void Log(ILoggerWrapper log, Level defaultLevel, Func<string> generator)
        {
            var level = GetLevel(log, defaultLevel);
            var message = generator();
            log.Logger.Log(typeof(LogExtensions), level, message, null);
        }

        private static Level GetLevel(ILoggerWrapper log, Level defaultLevel)
        {
            return log.Logger.Repository.LevelMap.LookupWithDefault(defaultLevel);
        }
    }
}
