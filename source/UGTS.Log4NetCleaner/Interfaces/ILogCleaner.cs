using System;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace UGTS.Log4NetCleaner.Interfaces
{
    public interface ILogCleaner
    {
        string BasePath { get; set; }
        IDirectoryCleaner DirectoryCleaner { get; set; }
        string FileExtension { get; set; }
        string MaximumDirectorySize { get; set; }
        string MaximumFileAgeDays { get; set; }
        long? MaxDirectorySize { get; set; }
        double? MaxFileAgeDays { get; set; }
        double PeriodMinutes { get; set; }
        ITaskRunner TaskRunner { get; set; }
        WaitType WaitType { get; set; }

        Task Cleanup();
        void InferFileExtension(string path);
        bool IsDueForCleaning(DateTime now);
        void TryCleanup();
    }
}