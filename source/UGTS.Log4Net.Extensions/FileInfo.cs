using System;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions
{
    public class FileInfo
    {
        public DateTime LastWriteTimeUtc { get; set; }
        public long Length { get; set; }
    }
}