using System;
using log4net.Appender;

namespace UGTS.Log4NetCleaner
{
    internal class UniversalDateTime : RollingFileAppender.IDateTime
    {
        public DateTime Now => DateTime.UtcNow;
    }
}