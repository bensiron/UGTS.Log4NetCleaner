using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net.Appender;
using log4net.Core;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions
{
    public class SelfCleaningRollingFileAppender : RollingFileAppender, ISelfCleaningRollingFileAppender
    {
        private const double DefaultCleaningPeriodMinutes = 60.0;

        public SelfCleaningRollingFileAppender()
        {
            _self = this;
            DateTimeProvider = new UniversalDateTime();
            Cleaner = new DirectoryCleaner(new FileSystemOperations(new FileSystem()), DateTimeProvider);
            TaskRunner = new TaskRunner();
        }

        private readonly ISelfCleaningRollingFileAppender _self; // for unit testing private calls by the instance to itself

        public DateTime? LastCleaning;

        [UsedImplicitly] public IDirectoryCleaner Cleaner { get; set; }

        [UsedImplicitly] public IDateTime DateTimeProvider { get; set; }

        [UsedImplicitly] public ITaskRunner TaskRunner { get; set; }

        [UsedImplicitly] public string CleaningBasePath { get; set; }

        [UsedImplicitly] public double MaxAgeDays { get; set; } = double.MaxValue;

        [UsedImplicitly] public long MaxSizeBytes { get; set; } = long.MaxValue;

        [UsedImplicitly] public double CleaningPeriodMinutes { get; set; } = DefaultCleaningPeriodMinutes;

        [UsedImplicitly] public CleaningWaitType CleaningWaitType { get; set; } = CleaningWaitType.FirstTimeOnly;

        public override void ActivateOptions()
        {
            if (CleaningBasePath == null) CleaningBasePath = File;
            _self.ActivateOptionsBase();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            _self.TryCleanupLogDirectory();
            _self.AppendBase(loggingEvent);
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            _self.TryCleanupLogDirectory();
            _self.AppendBase(loggingEvents);
        }

        public void TryCleanupLogDirectory()
        {
            if (!HasMaxAgeDays && !HasMaxSizeBytes) return;
            var wasFirstTime = LastCleaning == null;

            var now = DateTimeProvider.Now;
            if (!_self.IsDueForCleaning(now)) return;

            LastCleaning = Cleaner.UpdateLastCleaningTime(CleaningBasePath);
            var task = _self.CleanupLogDirectory();
            if (_self.ShouldWaitForCleaning(wasFirstTime)) task.Wait();
        }

        public Task CleanupLogDirectory()
        {
            var now = DateTimeProvider.Now;
            var cutoffDate = HasMaxAgeDays ? (DateTime?)now.AddDays(-MaxAgeDays) : null;
            var fileExtension = System.IO.Path.GetExtension(File);
            return TaskRunner.Run(() => Cleaner.Clean(CleaningBasePath, fileExtension, cutoffDate, HasMaxSizeBytes ? (long?)MaxSizeBytes : null));
        }

        public bool IsDueForCleaning(DateTime now)
        {
            if (!LastCleaning.HasValue)
                LastCleaning = Cleaner.GetLastCleaningTime(CleaningBasePath) ?? DateTime.MinValue;

            return (now - LastCleaning.Value).TotalMinutes >= CleaningPeriodMinutes;
        }

        bool ISelfCleaningRollingFileAppender.ShouldWaitForCleaning(bool wasFirstTime)
        {
            return (CleaningWaitType != CleaningWaitType.Never) &&
               (wasFirstTime || CleaningWaitType == CleaningWaitType.Always);
        }

        void ISelfCleaningRollingFileAppender.ActivateOptionsBase()
        {
            base.ActivateOptions();
        }

        void ISelfCleaningRollingFileAppender.AppendBase(LoggingEvent loggingEvent)
        {
            base.Append(loggingEvent);
        }

        void ISelfCleaningRollingFileAppender.AppendBase(LoggingEvent[] loggingEvents)
        {
            base.Append(loggingEvents);
        }

        private bool HasMaxAgeDays => MaxAgeDays <= 400000;

        private bool HasMaxSizeBytes => MaxSizeBytes < long.MaxValue;
    }
}
