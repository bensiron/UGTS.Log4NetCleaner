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
  
In this example, the only new definitions beyond what RollingFileAppender uses are: the type of the appender, and the cleaner tag with the properties defined under it.  This example will clean out any log files older than 90 days and will remove the oldest log files when the total size of all log files exceeds 100 megabytes.

The properties defined under the cleaner tag include:

- fileExtension:
        Use this to restrict which files by file extension will be cleaned up.
        If this property is omitted, the file extension will be inferred from the extension of the log files created.  Usually inference works well enough so that you need not specify this property.
        You can explicitly set this property to * or blank to clean all files whatever the file extension or lack thereof.
        The lastcleaning.check file is never removed regardless of the value of this property.
        This extension can include or omit a leading dot, it will not affect the results, and the extension is not case sensititve.
        Rolling backup files ending in .ext.N will also be removed along with files ending in .ext
        For example, if this property has value 'txt', then the log files app.txt and app.txt.14 would be removed but not app.log or app.log.14 or app.txt.log.

- maximumDirectorySize: 
        If this value is specified, then log files (from oldest to newest) will be deleted until the total size
        of log files is less than the directory maximum.  This parameter must be an integer optionally suffixed 
        with KB, MB, or GB.  For example, 100MB specifies a maxmimum directory size of 100 megabytes.
        Either this value or MaximumFileAgeDays must be specified or no cleaning will be performed.
        If this value is blank, then cleaning will only be done according to the MaximumFileAgeDays parameter.

- maximumFileAgeDays:
        Either this value or MaximumDirectorySize must be specified or no cleaning will be performed.
        If this value is blank, then cleaning will only be done according to the MaximumDirectorySize parameter.
       
 
  


