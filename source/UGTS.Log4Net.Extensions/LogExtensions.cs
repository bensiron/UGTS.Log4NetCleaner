using System;
using JetBrains.Annotations;
using log4net;

namespace UGTS.Log4Net.Extensions
{
    public static class LogExtensions
    {
        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Fatal(this ILog log, Func<string> generator)
        {
            if (log.IsFatalEnabled) log.Fatal(generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Error(this ILog log, Func<string> generator)
        {
            if (log.IsErrorEnabled) log.Error(generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Warn(this ILog log, Func<string> generator)
        {
            if (log.IsWarnEnabled) log.Warn(generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Info(this ILog log, Func<string> generator)
        {
            if (log.IsInfoEnabled) log.Info(generator());
        }

        /// <summary>
        /// Syntactic sugar for if (log.Is[Level]Enabled) log.[Level](generator());
        /// Note that using this invalidates %file, %method, %line log output, but not %stacktrace/detail
        /// </summary>
        [UsedImplicitly]
        public static void Debug(this ILog log, Func<string> generator)
        {
            if (log.IsDebugEnabled) log.Debug(generator());
        }
    }
}
