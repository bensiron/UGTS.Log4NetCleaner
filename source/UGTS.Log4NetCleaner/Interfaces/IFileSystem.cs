using System.Collections.Generic;
#pragma warning disable 1591

namespace UGTS.Log4NetCleaner.Interfaces
{
    public interface IFileSystem
    {
        IEnumerable<string> GetDirectories(string path);
        IEnumerable<string> GetFiles(string path);
        void DeleteFile(string path);
        void DeleteDirectory(string path);
        bool ExistsDirectory(string path);
        FileInfo GetFileInfo(string path);
        void CreateEmptyFile(string path);
    }
}
