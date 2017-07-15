using System.Collections.Generic;
using System.IO;
using UGTS.Log4NetCleaner.Interfaces;
#pragma warning disable 1591

namespace UGTS.Log4NetCleaner
{
    public class FileSystem : IFileSystem
    {
        public IEnumerable<string> GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return Directory.GetFiles(path);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public void DeleteDirectory(string path)
        {
            Directory.Delete(path, true);
        }

        public bool ExistsDirectory(string path)
        {
            return Directory.Exists(path);
        }

        public FileInfo GetFileInfo(string path)
        {
            var info = new System.IO.FileInfo(path);
            return new FileInfo { LastWriteTimeUtc = info.LastWriteTimeUtc, Length = info.Length };
        }

        public void CreateEmptyFile(string path)
        {
            File.WriteAllBytes(path, new byte[0]);
        }
    }
}
