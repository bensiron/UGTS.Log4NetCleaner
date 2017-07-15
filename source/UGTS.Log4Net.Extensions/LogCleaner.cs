using System;
using System.Globalization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net.Appender;
using log4net.Util;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// </summary>
    public class LogCleaner : ILogCleaner
    {
        private const double DefaultCleaningPeriodMinutes = 480.0;

        private long? _maximumDirectorySizeBytes;
        private double? _maximumFileAgeDays;
        private readonly RollingFileAppender.IDateTime _dateTimeProvider;

        private readonly ILogCleaner _self; // for unit testing private calls by the instance to itself

        /// <summary>
        /// </summary>
        public LogCleaner()
        {
            _self = this;
            _dateTimeProvider = new UniversalDateTime();
            DirectoryCleaner = new DirectoryCleaner(new FileSystemOperations(new FileSystem()), _dateTimeProvider);
            TaskRunner = new TaskRunner();
        }

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
        public IDirectoryCleaner DirectoryCleaner { get; set; }

        /// <summary>
        /// Gets or sets the implementation of the cleaning task runner
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation simply takes the action and schedules it to run it on a new thread.
        /// Use this to inject an alternative implementation.
        /// </para>
        /// </remarks>
        public ITaskRunner TaskRunner { get; set; }

        /// <summary>
        /// Gets or sets the root directory of the log files for cleaning.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is omitted, it will be inferred from the File property.
        /// Sometimes inference does not work if the File is not a directory but also contains
        /// part of the file name.
        /// Use care when setting this property and the FileExtension.  The appender
        /// will clean any directory you give it, and with a FileExtension of * this can 
        /// result in non-log files being removed.
        /// </para>
        /// </remarks>
        public string BasePath { get; set; }

        /// <summary>
        /// Gets or sets the filename extension of the log files to remove.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can be explicitly set to * to clean all files whatever the file extension or lack thereof.
        /// The lastcleaning.check file is never removed regardless of this property.
        /// If this property is omitted, the file extension will be inferred from the 
        /// extension of the log files created.  If the file extension cannot be inferred, then cleaning will not be performed.  
        /// Usually inference works well enough so that you need not specify this property,
        /// but if your log file naming convention is complex enough, you may need to explicitly set this property.
        /// This extension can include or omit a leading dot, it will not affect the results,
        /// and the extension is not case sensititve.
        /// Rolling backup files ending in .ext.N will also be removed along with files ending in .ext
        /// For example, if this property has value 'txt', then the log files app.txt and app.txt.14 would be removed
        /// but not app.log or app.log.14 or app.txt.log.
        /// </para>
        /// </remarks>
        public string FileExtension { get; set; }

        /// <summary>
        /// Gets or sets the maximum age of log files (in a decimal number of days) to keep when cleaning the log directory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Either this value or MaximumDirectorySize must be specified or no cleaning will be performed.
        /// If this value is blank, then cleaning will only be done according to the MaximumDirectorySize parameter.
        /// </para>
        /// </remarks>
        public string MaximumFileAgeDays
        {
            get { return _maximumFileAgeDays?.ToString() ?? ""; }
            set
            {
                double days;
                _maximumFileAgeDays = double.TryParse(value, out days) ? (double?)days : null;
            }
        }

        /// <summary>
        /// The numeric equivalent of MaximumFileAgeDays
        /// </summary>
        public double? MaxFileAgeDays
        {
            get { return _maximumFileAgeDays; }
            set { _maximumFileAgeDays = value; }
        }

        /// <summary>
        /// Gets or sets the maximum allowed size of the log directory in bytes for all log files found.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is specified, then log files (from oldest to newest) will be deleted until the total size
        /// of log files is less than the directory maximum.  This parameter must be an integer optionally suffixed 
        /// with KB, MB, or GB.  For example, 100MB specifies a maxmimum directory size of 100 megabytes.
        /// Either this value or MaximumFileAgeDays must be specified or no cleaning will be performed.
        /// If this value is blank, then cleaning will only be done according to the MaximumFileAgeDays parameter.
        /// </para>
        /// </remarks>
        public string MaximumDirectorySize
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
        /// The numeric equivalent of the string property MaximumDirectorySize
        /// </summary>
        [UsedImplicitly]
        public long? MaxDirectorySize
        {
            get { return _maximumDirectorySizeBytes; }
            set { _maximumDirectorySizeBytes = value; }
        }

        /// <summary>
        /// Gets or sets the decimal number of minutes to wait between directory cleaning checks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This period defaults to 480 minutes if not specified.  Cleaning is performed at the first logging call where it has
        /// been at least this many minutes since the last cleaning.  The date of the last cleaning is stored between process runs
        /// by using the last modified date (UTC) of the lastcleaning.check file which is placed at the root of the log directory.
        /// </para>
        /// </remarks>
        public double PeriodMinutes { get; set; } = DefaultCleaningPeriodMinutes;

        /// <summary>
        /// Gets or sets the type of waiting to do when cleaning the log directory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can be either: Never or Always
        /// If the value is Always (default), then log directory cleaning will run on the same thread as logging, and will block until cleaning is complete.  This is recommended for batch and other background jobs.
        /// If the value is Never, then cleaning is performed asynchronously in the background on a different thread.  This is recommended for web and other processes which run continuously.
        /// </para>
        /// </remarks>
        public WaitType WaitType { get; set; } = WaitType.Always;

        /// <summary>
        /// meant for internal use only
        /// </summary>
        public void TryCleanup()
        {
            if (!(MaxFileAgeDays.HasValue || MaxDirectorySize.HasValue)) return;
            if (string.IsNullOrWhiteSpace(FileExtension)) return;

            var now = _dateTimeProvider.Now;
            if (!_self.IsDueForCleaning(now)) return;

            LastCleaning = DirectoryCleaner.UpdateLastCleaningTime(BasePath);
            _self.Cleanup();
        }

        /// <summary>
        /// call this to explicitly trigger a cleaning of the log directory
        /// </summary>
        public Task Cleanup()
        {
            var now = _dateTimeProvider.Now;
            var cutoffDate = MaxFileAgeDays.HasValue ? (DateTime?)now.AddDays(-MaxFileAgeDays.Value) : null;
            return TaskRunner.Run(() => DirectoryCleaner.Clean(BasePath, FileExtension, cutoffDate, MaxDirectorySize), WaitType);
        }

        /// <summary>
        /// true if the next logging call will trigger a cleaning of the log directory
        /// </summary>
        public bool IsDueForCleaning(DateTime now)
        {
            if (!LastCleaning.HasValue)
            {
                LastCleaning = DirectoryCleaner.GetLastCleaningTime(BasePath) ?? DateTime.MinValue;
            }

            return (now - LastCleaning.Value).TotalMinutes >= PeriodMinutes;
        }

        /// <summary>
        /// Infers the file extension from the given log file
        /// </summary>
        public void InferFileExtension(string path)
        {
            FileExtension = DirectoryCleaner.GetFileExtension(path);
            if (string.IsNullOrWhiteSpace(FileExtension))
            {
                LogLog.Warn(typeof (LogCleaner),
                    $"Could not infer FileExtension property for log file cleaning - no file extension was specified, and the log file '{path}' does not have a file extension.");
            }
        }
    }
}