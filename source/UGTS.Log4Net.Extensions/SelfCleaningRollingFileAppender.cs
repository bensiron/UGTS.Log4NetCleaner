using System;
using System.Globalization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions
{
    public class SelfCleaningRollingFileAppender : RollingFileAppender, ISelfCleaningRollingFileAppender
    {
        private const double DefaultCleaningPeriodMinutes = 60.0;

        public SelfCleaningRollingFileAppender()
        {
            _self = this;
            _dateTimeProvider = new UniversalDateTime();
            Cleaner = new DirectoryCleaner(new FileSystemOperations(new FileSystem()), _dateTimeProvider);
            TaskRunner = new TaskRunner();
        }

        private long? _maximumDirectorySizeBytes;
        private double? _maxmimumFileAgeDays;

        private readonly ISelfCleaningRollingFileAppender _self; // for unit testing private calls by the instance to itself

        private readonly IDateTime _dateTimeProvider;


        public DateTime? LastCleaning;

        [UsedImplicitly] public IDirectoryCleaner Cleaner { get; set; }

        [UsedImplicitly] public ITaskRunner TaskRunner { get; set; }

        [UsedImplicitly] public string CleaningBasePath { get; set; }

        [UsedImplicitly] public string CleaningFileExtension { get; set; }

        [UsedImplicitly]
        public string CleaningMaximumFileAgeDays
        {
            get
            {
                return _maxmimumFileAgeDays?.ToString() ?? "";
            }
            set
            {
                double days;
                _maxmimumFileAgeDays = double.TryParse(value, out days) ? days : (double?) null;
            }
        }

        [UsedImplicitly]
        public string CleaningMaximumDirectorySize
        {
            get
            {
                return _maximumDirectorySizeBytes?.ToString(NumberFormatInfo.InvariantInfo) ?? "";
            }
            set
            {
                _maximumDirectorySizeBytes = OptionConverter.ToFileSize(value, -1);
                if (_maximumDirectorySizeBytes <= 0) _maximumDirectorySizeBytes = null;
            }
        }

        [UsedImplicitly] public double CleaningPeriodMinutes { get; set; } = DefaultCleaningPeriodMinutes;

        [UsedImplicitly] public CleaningWaitType CleaningWaitType { get; set; } = CleaningWaitType.FirstTimeOnly;

        public override void ActivateOptions()
        {
            if (CleaningBasePath == null) CleaningBasePath = File;
            _self.ActivateOptionsBase();
            if (CleaningFileExtension == null) CleaningFileExtension = Cleaner.GetFileExtension(File);
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
            if (!_maxmimumFileAgeDays.HasValue && !_maximumDirectorySizeBytes.HasValue) return;
            var wasFirstTime = LastCleaning == null;

            var now = _dateTimeProvider.Now;
            if (!_self.IsDueForCleaning(now)) return;

            LastCleaning = Cleaner.UpdateLastCleaningTime(CleaningBasePath);
            var task = _self.CleanupLogDirectory();
            if (_self.ShouldWaitForCleaning(wasFirstTime)) task.Wait();
        }

        public Task CleanupLogDirectory()
        {
            var now = _dateTimeProvider.Now;
            var cutoffDate = _maxmimumFileAgeDays.HasValue ? (DateTime?)now.AddDays(-_maxmimumFileAgeDays.Value) : null;
            return TaskRunner.Run(() => Cleaner.Clean(CleaningBasePath, CleaningFileExtension, cutoffDate, _maximumDirectorySizeBytes));
        }

        public bool IsDueForCleaning(DateTime now)
        {
            if (!LastCleaning.HasValue)
            {
                LastCleaning = Cleaner.GetLastCleaningTime(CleaningBasePath) ?? DateTime.MinValue;
            }

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
    }
}
