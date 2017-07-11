using System;
using System.Collections.Generic;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface IFileSystemOperations
    {
        bool ExistsDirectory(string path);
        IDictionary<string, FileInfo> FindFileInfo(string path, Func<string, bool> matcher);
        void DeleteFile(string path);
        void DeleteEmptyDirectories(string path);
        DateTime? CreateEmptyFile(string path);
        FileInfo GetFileInfo(string path);
    }
}