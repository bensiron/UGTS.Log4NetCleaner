using System;
using System.Threading.Tasks;
using log4net.Core;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface ISelfCleaningRollingFileAppender
    {
        void AppendBase(LoggingEvent loggingEvent);
        void AppendBase(LoggingEvent[] loggingEvents);
        void ActivateOptionsBase();
        void TryCleanupLogDirectory();
        Task CleanupLogDirectory(bool wait);
        bool IsDueForCleaning(DateTime now);
        bool ShouldWaitForCleaning();
    }
}