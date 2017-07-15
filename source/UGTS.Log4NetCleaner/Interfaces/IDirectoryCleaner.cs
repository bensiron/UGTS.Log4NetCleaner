using System;
#pragma warning disable 1591

namespace UGTS.Log4NetCleaner.Interfaces
{
    public interface IDirectoryCleaner
    {
        void Clean(string path, string fileExtension, DateTime? cutoffTime, long? maxSizeBytes);
        DateTime? GetLastCleaningTime(string path);
        DateTime UpdateLastCleaningTime(string path);
        string GetFileExtension(string path);
    }
}