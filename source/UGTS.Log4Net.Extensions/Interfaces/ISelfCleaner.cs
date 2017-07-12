using System;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface ISelfCleaner
    {
        string BasePath { get; set; }
        IDirectoryCleaner DirectoryCleaner { get; set; }
        string FileExtension { get; set; }
        string MaximumDirectorySize { get; set; }
        double MaximumFileAgeDays { get; set; }
        double PeriodMinutes { get; set; }
        ITaskRunner TaskRunner { get; set; }
        WaitType WaitType { get; set; }

        Task Cleanup();
        void InferFileExtension(string path);
        bool IsDueForCleaning(DateTime now);
        void TryCleanup();
    }
}