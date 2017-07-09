using System;

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface IDirectoryCleaner
    {
        void Clean(string path, string fileExtension, DateTime? cutoffTime, long? maxSizeBytes);
        DateTime? GetLastCleaningTime(string path);
        DateTime UpdateLastCleaningTime(string path);
    }
}