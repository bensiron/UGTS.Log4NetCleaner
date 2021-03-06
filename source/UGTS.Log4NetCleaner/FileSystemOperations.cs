﻿using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Util;
using UGTS.Log4NetCleaner.Interfaces;
#pragma warning disable 1591

namespace UGTS.Log4NetCleaner
{
    public class FileSystemOperations : IFileSystemOperations
    {
        private readonly IFileSystem _fs;

        public FileSystemOperations(IFileSystem fs)
        {
            _fs = fs;
        }

        public bool ExistsDirectory(string path)
        {
            return _fs.ExistsDirectory(path);
        }

        public IDictionary<string, FileInfo> FindFileInfo(string path, Func<string, bool> matcher)
        {
            var found = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
            RecursivelyFindFileInfo(path, matcher, found);
            return found;
        }

        public void DeleteFile(string path)
        {
            try
            {
                _fs.DeleteFile(path);
            }
            catch (Exception e)
            {
                LogLog.Error(typeof(FileSystemOperations), $"Could not delete file at '{path}", e);
            }
        }

        public void DeleteEmptyDirectories(string path)
        {
            RecursivelyRemoveEmptyDirectories(path);
        }

        public DateTime? CreateEmptyFile(string path)
        {
            try
            {
                _fs.CreateEmptyFile(path);
                return _fs.GetFileInfo(path).LastWriteTimeUtc;
            }
            catch (Exception e)
            {
                LogLog.Error(typeof(FileSystemOperations), $"Could not create empty file at '{path}", e);
                return null;
            }
        }

        public FileInfo GetFileInfo(string path)
        {
            try
            {
                return _fs.GetFileInfo(path);
            }
            catch (Exception e)
            {
                LogLog.Error(typeof(FileSystemOperations), $"Could not get file info for '{path}", e);
                return null;
            }
        }

        private void RecursivelyFindFileInfo(string path, Func<string, bool> matcher, IDictionary<string, FileInfo> found)
        {
            foreach (var subPath in _fs.GetDirectories(path))
            {
                RecursivelyFindFileInfo(subPath, matcher, found);
            }

            foreach (var filePath in _fs.GetFiles(path))
            {
                if (!matcher(filePath)) continue;
                found[filePath] = _fs.GetFileInfo(filePath);
            }
        }

        private bool RecursivelyRemoveEmptyDirectories(string path)
        {
            var oks = _fs.GetDirectories(path).Select(RecursivelyRemoveEmptyDirectories).ToList();
            if (!oks.All(ok => ok))
                return false;

            if (_fs.GetFiles(path).Any())
                return false;

            try
            {
                _fs.DeleteDirectory(path);
                return true;
            }
            catch (Exception e)
            {
                LogLog.Error(typeof(FileSystemOperations), $"Could not delete directory '{path}", e);
                return false;
            }
        }
    }
}
