using log4net.Appender;
using log4net.Core;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// A self-cleaning rolling file appender which can clean old files out of its log output directory.
    /// </summary>
    public class SelfCleaningRollingFileAppender : RollingFileAppender, ISelfCleaningRollingFileAppender
    {
        private readonly ISelfCleaningRollingFileAppender _self; // for unit testing private calls by the instance to itself

        /// <summary>
        /// </summary>
        public SelfCleaningRollingFileAppender()
        {
            _self = this;
        }

        /// <summary>
        /// Sets the options used for cleaning log files
        /// </summary>
        public ILogCleaner Cleaner { get; set; } = new LogCleaner();

        /// <summary>
        /// meant for internal use only
        /// </summary>
        public override void ActivateOptions()
        {
            if (Cleaner.BasePath == null) Cleaner.BasePath = File;
            _self.ActivateOptionsBase();
            if (Cleaner.FileExtension == null) Cleaner.InferFileExtension(File);
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        protected override void Append(LoggingEvent loggingEvent)
        {
            Cleaner.TryCleanup();
            _self.AppendBase(loggingEvent);
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        protected override void Append(LoggingEvent[] loggingEvents)
        {
            Cleaner.TryCleanup();
            _self.AppendBase(loggingEvents);
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        void ISelfCleaningRollingFileAppender.ActivateOptionsBase()
        {
            base.ActivateOptions();
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        void ISelfCleaningRollingFileAppender.AppendBase(LoggingEvent loggingEvent)
        {
            base.Append(loggingEvent);
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        void ISelfCleaningRollingFileAppender.AppendBase(LoggingEvent[] loggingEvents)
        {
            base.Append(loggingEvents);
        }
    }
}
