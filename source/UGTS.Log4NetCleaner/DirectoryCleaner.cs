using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using log4net.Appender;
using UGTS.Log4NetCleaner.Interfaces;
#pragma warning disable 1591

namespace UGTS.Log4NetCleaner
{
    public class DirectoryCleaner : IDirectoryCleaner
    {
        private readonly IFileSystemOperations _ops;
        private readonly RollingFileAppender.IDateTime _time;
        private readonly Regex _backupRegex = new Regex("\\.\\d+$");

        public const string LastCleaningCheckFileName = "lastcleaning.check";

        public DirectoryCleaner(IFileSystemOperations ops, RollingFileAppender.IDateTime time)
        {
            _ops = ops;
            _time = time;
        }

        public void Clean(string path, string fileExtension, DateTime? cutoffTime, long? maxSizeBytes)
        {
            if (!_ops.ExistsDirectory(path)) return;

            var found = _ops.FindFileInfo(path, p => IsMatchingLogFile(p, fileExtension));

            if (cutoffTime.HasValue)
                RemoveFiles(found, found.Keys, info => info.LastWriteTimeUtc < cutoffTime.Value);

            if (maxSizeBytes.HasValue)
                RemoveOldestFilesOverSizeLimit(found, maxSizeBytes.Value);

            _ops.DeleteEmptyDirectories(path);
        }

        public DateTime? GetLastCleaningTime(string path)
        {
            return _ops.GetFileInfo(GetLastCleaningFilePath(path))?.LastWriteTimeUtc;
        }

        public DateTime UpdateLastCleaningTime(string path)
        {
            return _ops.CreateEmptyFile(GetLastCleaningFilePath(path)) ?? _time.Now;
        }

        public string GetFileExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            if (_backupRegex.IsMatch(path)) path = path.Substring(0, path.LastIndexOf('.'));
            return Path.GetExtension(path);
        }

        private bool IsMatchingLogFile(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (string.IsNullOrWhiteSpace(extension)) return false;
            if (string.Equals(Path.GetFileName(path), LastCleaningCheckFileName, StringComparison.OrdinalIgnoreCase)) return false;
            if (extension == "*") return true;

            if (!extension.StartsWith(".")) extension = "." + extension;

            return string.Equals(extension, GetFileExtension(path), StringComparison.OrdinalIgnoreCase);
        }

        private void RemoveOldestFilesOverSizeLimit(IDictionary<string, FileInfo> found, long limit)
        {
            var totalSizeRemaining = found.Values.Sum(info => info.Length);
            RemoveFiles(found, found.Keys.OrderBy(f => found[f].LastWriteTimeUtc),
                info =>
                {
                    var remove = totalSizeRemaining > limit;
                    totalSizeRemaining -= info.Length;
                    return remove;
                });
        }

        private void RemoveFiles(IDictionary<string, FileInfo> found, IEnumerable<string> files, Func<FileInfo, bool> predicate)
        {
            var remove = files.Where(file =>
            {
                var info = found[file];
                return predicate(info);
            }).ToList();
 
            foreach (var file in remove)
            {
                _ops.DeleteFile(file);
                found.Remove(file);
            }
        }

        private static string GetLastCleaningFilePath(string path)
        {            
            return Path.Combine(path, LastCleaningCheckFileName);
        }
    }
}
