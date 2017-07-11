﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// A self-cleaning rolling file appender which can clean old files out of its log output directory.
    /// </summary>
    public class SelfCleaningRollingFileAppender : RollingFileAppender, ISelfCleaningRollingFileAppender
    {
        private const double DefaultCleaningPeriodMinutes = 60.0;

        /// <summary>
        /// The one and only constructor for this type
        /// </summary>
        public SelfCleaningRollingFileAppender()
        {
            _self = this;
            _dateTimeProvider = new UniversalDateTime();
            Cleaner = new DirectoryCleaner(new FileSystemOperations(new FileSystem()), _dateTimeProvider);
            CleaningTaskRunner = new TaskRunner();
        }

        private long? _maximumDirectorySizeBytes;
        private double? _maxmimumFileAgeDays;

        private readonly ISelfCleaningRollingFileAppender _self; // for unit testing private calls by the instance to itself

        private readonly IDateTime _dateTimeProvider;

        /// <summary>
        /// Gets or sets the last time that the log directory was cleaned.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Clear this to trigger a log directory cleaning at the next logging call.
        /// </para>
        /// </remarks>
        public DateTime? LastCleaning;

        /// <summary>
        /// Gets or sets the implementation of the directory cleaner.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this to inject your own implementation of directory cleaning.
        /// </para>
        /// </remarks>
        [UsedImplicitly] public IDirectoryCleaner Cleaner { get; set; }

        /// <summary>
        /// Gets or sets the implementation of the cleaning task runner
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation simply takes the action and schedules it to run it on a new thread.
        /// Use this to inject an alternative implementation.
        /// </para>
        /// </remarks>
        [UsedImplicitly] public ITaskRunner CleaningTaskRunner { get; set; }

        /// <summary>
        /// Gets or sets the root directory of the log files for cleaning.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is omitted, it will be inferred from the File property.
        /// Sometimes inference does not work if the File is not a directory but also contains
        /// part of the file name.
        /// Use care when setting this property and the CleaningFileExtension.  The appender
        /// will clean any directory you give it, and with a CleaningFileExtension of * this can 
        /// result in non-log files being removed.
        /// </para>
        /// </remarks>
        [UsedImplicitly] public string CleaningBasePath { get; set; }

        /// <summary>
        /// Gets or sets the filename extension of the log files to remove.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can be * or blank to clean all files whatever the file extension or lack thereof.
        /// The lastcleaning.check file is never removed regardless of this property.
        /// If this property is omitted, the file extension will be inferred from the 
        /// extension of the log files created.  Usually inference works well enough so that you need not specify this property,
        /// but if your log file naming convention is complex enough, you may need to explicitly set this property.
        /// This extension can include or omit a leading dot, it will not affect the results,
        /// and the extension is not case sensititve.
        /// Rolling backup files ending in .ext.N will also be removed along with files ending in .ext
        /// For example, if this property has value 'txt', then the log files app.txt and app.txt.14 would be removed
        /// but not app.log or app.log.14 or app.txt.log.
        /// </para>
        /// </remarks>
        [UsedImplicitly] public string CleaningFileExtension { get; set; }

        /// <summary>
        /// Gets or sets the maximum age of log files (in a decimal number of days) to keep when cleaning the log directory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Either this value or CleaningMaximumDirectorySize must be specified or no cleaning will be performed.
        /// If this value is blank, then cleaning will only be done according to the CleaningMaximumDirectorySize parameter.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Gets or sets the maximum allowed size of the log directory in bytes for all log files found.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is specified, then log files (from oldest to newest) will be deleted until the total size
        /// of log files is less than the directory maximum.  This parameter must be an integer optionally suffixed 
        /// with KB, MB, or GB.  For example, 100MB specifies a maxmimum directory size of 100 megabytes.
        /// Either this value or CleaningMaximumFileAgeDays must be specified or no cleaning will be performed.
        /// If this value is blank, then cleaning will only be done according to the CleaningMaximumFileAgeDays parameter.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Gets or sets the decimal number of minutes to wait between directory cleaning checks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This period defaults to 60 minutes if not specified.  Cleaning is performed at the first logging call where it has
        /// been at least this many minutes since the last cleaning.  The date of the last cleaning is stored between process runs
        /// by using the last modified date (UTC) of the lastcleaning.check file which is placed at the root of the log directory.
        /// </para>
        /// </remarks>
        [UsedImplicitly] public double CleaningPeriodMinutes { get; set; } = DefaultCleaningPeriodMinutes;

        /// <summary>
        /// Gets or sets the type of waiting to do when cleaning the log directory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can take three different values: Never, Always, or FirstTimeOnly (default).
        /// If the value is Always, then the logging call will block until cleaning is complete.  This is recommended for batch jobs.
        /// If the value is Never, then cleaning is always performed asynchronously in the background on a different thread.  This is recommended for web and other servers which run continuously.
        /// If the value is FirstTimeOnly, then cleaning will only be done synchronously if it is triggered by the first logging call for this process run.  This is the default,
        /// and is used as a compromise to ensure that cleaning is performed periodically to completion, yet preferring to run in the background.
        /// </para>
        /// </remarks>
        [UsedImplicitly] public CleaningWaitType CleaningWaitType { get; set; } = CleaningWaitType.FirstTimeOnly;

        /// <summary>
        /// meant for internal use only
        /// </summary>
        public override void ActivateOptions()
        {
            if (CleaningBasePath == null) CleaningBasePath = File;
            _self.ActivateOptionsBase();
            if (CleaningFileExtension == null) CleaningFileExtension = Cleaner.GetFileExtension(File);
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        protected override void Append(LoggingEvent loggingEvent)
        {
            _self.TryCleanupLogDirectory();
            _self.AppendBase(loggingEvent);
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        protected override void Append(LoggingEvent[] loggingEvents)
        {
            _self.TryCleanupLogDirectory();
            _self.AppendBase(loggingEvents);
        }

        /// <summary>
        /// meant for internal use only
        /// </summary>
        public void TryCleanupLogDirectory()
        {
            if (!_maxmimumFileAgeDays.HasValue && !_maximumDirectorySizeBytes.HasValue) return;
            var wait = _self.ShouldWaitForCleaning();

            var now = _dateTimeProvider.Now;
            if (!_self.IsDueForCleaning(now)) return;

            LastCleaning = Cleaner.UpdateLastCleaningTime(CleaningBasePath);
            _self.CleanupLogDirectory(wait);
        }

        /// <summary>
        /// call this to explicitly trigger a cleaning of the log directory
        /// </summary>
        public Task CleanupLogDirectory(bool wait)
        {
            var now = _dateTimeProvider.Now;
            var cutoffDate = _maxmimumFileAgeDays.HasValue ? (DateTime?)now.AddDays(-_maxmimumFileAgeDays.Value) : null;
            return CleaningTaskRunner.Run(() => Cleaner.Clean(CleaningBasePath, CleaningFileExtension, cutoffDate, _maximumDirectorySizeBytes), wait);
        }

        /// <summary>
        /// true if the next logging call will trigger a cleaning of the log directory
        /// </summary>
        public bool IsDueForCleaning(DateTime now)
        {
            if (!LastCleaning.HasValue)
            {
                LastCleaning = Cleaner.GetLastCleaningTime(CleaningBasePath) ?? DateTime.MinValue;
            }

            return (now - LastCleaning.Value).TotalMinutes >= CleaningPeriodMinutes;
        }

        /// <summary>
        /// true if log cleaning should run synchronously
        /// </summary>
        bool ISelfCleaningRollingFileAppender.ShouldWaitForCleaning()
        {
            return (CleaningWaitType != CleaningWaitType.Never) &&
               (LastCleaning == null || CleaningWaitType == CleaningWaitType.Always);
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
