# UGTS.Log4Net.Extensions
Provides the SelfCleaningRollingFileAppender log4net class, extensions methods, and the 'reqid' log pattern key which logs a unique id for each unique web request.

This library requires log4net 2.0.8 or higher and .NET 4.5 or higher.  .NET Core/Standard libraries are not supported.

The SelfCleaningRollingFileAppender is a RollingFileAppender which periodically removes log files from the output log directory more than a specified number of days in age, or removes files when the total size of the log directory exceeds a threshold.  Here is an example config section which shows how this appender can be configured:

```xml
  <log4net debug="true">
    <appender name="LogToFile" type="UGTS.Log4Net.Extensions.SelfCleaningRollingFileAppender, UGTS.Log4Net.Extensions">
      <file type="log4net.Util.PatternString" value="%env{LogPath}\\MyApp\\" />
      <DatePattern value="yyyyMM\\dd-HH'.log'" />
      <appendToFile value="true" />
      <cleaner type="UGTS.Log4Net.Extensions.LogCleaner">
        <maximumFileAgeDays value="90" />
        <maximumDirectorySize value="100MB" />
      </cleaner>
      <rollingStyle value="Date" />
      <staticLogFileName value="false" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%-5p %d{yyyy-MM-dd HH:mm:ss.fff} %-18.18c{1} - %m%n" />
      </layout>
    </appender>

    <root>
      <level value="DEBUG" />
      <appender-ref ref="LogToFile" />
    </root>
  </log4net>
```
  
In this example, the only new definitions beyond what RollingFileAppender does is the type of the appender, and the <cleaner> tag, with the properties defined under it.  This example will clean out any log files older than 90 days or will remove the oldest log files with the total size of all log files exceeds 100 megabytes.


