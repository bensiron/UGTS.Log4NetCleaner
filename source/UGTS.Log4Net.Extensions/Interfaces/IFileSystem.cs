using System.Collections.Generic;

namespace UGTS.Log4Net.Extensions.Interfaces
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
