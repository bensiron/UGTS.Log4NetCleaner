using log4net.Core;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface ISelfCleaningRollingFileAppender
    {
        void AppendBase(LoggingEvent loggingEvent);
        void AppendBase(LoggingEvent[] loggingEvents);
        void ActivateOptionsBase();
    }
}