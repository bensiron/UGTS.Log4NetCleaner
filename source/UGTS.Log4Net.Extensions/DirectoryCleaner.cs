using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net.Appender;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions
{
    public class DirectoryCleaner : IDirectoryCleaner
    {
        private readonly IFileSystemOperations _ops;
        private readonly RollingFileAppender.IDateTime _time;

        public DirectoryCleaner(IFileSystemOperations ops, RollingFileAppender.IDateTime time)
        {
            _ops = ops;
            _time = time;
        }

        public void Clean(string path, string fileExtension, DateTime? cutoffTime, long? maxSizeBytes)
        {
            if (string.IsNullOrWhiteSpace(fileExtension) || !_ops.ExistsDirectory(path)) return;

            var found = _ops.FindFileInfo(path,
                p => string.Equals(Path.GetExtension(p), fileExtension, StringComparison.OrdinalIgnoreCase));

            if (cutoffTime.HasValue)
                RemoveFiles(found, found.Keys, info => info.LastWriteTimeUtc < cutoffTime.Value);

            if (maxSizeBytes.HasValue)
                RemoveFilesOverSizeLimit(found, maxSizeBytes.Value);

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

        private void RemoveFilesOverSizeLimit(IDictionary<string, FileInfo> found, long limit)
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
            return Path.Combine(path, "lastcleaning.check");
        }
    }
}
