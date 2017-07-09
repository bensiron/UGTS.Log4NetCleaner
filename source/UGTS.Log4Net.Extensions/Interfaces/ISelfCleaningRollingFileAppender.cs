using System;
using System.Threading.Tasks;
using log4net.Core;

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface ISelfCleaningRollingFileAppender
    {
        void AppendBase(LoggingEvent loggingEvent);
        void AppendBase(LoggingEvent[] loggingEvents);
        void ActivateOptionsBase();
        void TryCleanupLogDirectory();
        Task CleanupLogDirectory();
        bool IsDueForCleaning(DateTime now);
        bool ShouldWaitForCleaning(bool wasFirstTime);
    }
}