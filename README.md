# UGTS.Log4NetCleaner
Provides the SelfCleaningRollingFileAppender log4net class.

This requires log4net 2.0.8 or higher and any target framework [compatible with .NET Standard 1.3](https://github.com/dotnet/standard/blob/master/docs/versions.md).

This is released under the MIT license.  Full details at /license/license.txt.

If you have any issues with this appender, would like to make requests, or submit/suggest improvements, please raise an issue.


## SelfCleaningRollingFileAppender

The SelfCleaningRollingFileAppender is a RollingFileAppender which periodically removes log files from the output log directory more than a specified number of days in age, or removes files when the total size of the log directory exceeds a threshold.  Here is an example config section which shows how this appender can be configured.

Note that it is important that both the appender type and cleaner type value be fully qualified names including the assembly name (as you see below), or you will get a type error.  The fully qualified name is required by the .NET Standard 1.3 build of log4net:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net"/>
  </configSections>

  <log4net debug="true">
    <appender name="LogToFile" type="UGTS.Log4NetCleaner.SelfCleaningRollingFileAppender, UGTS.Log4NetCleaner">
      <file type="log4net.Util.PatternString" value="%env{LogPath}\\MyApp\\" />
      <DatePattern value="yyyyMM\\dd-HH'.log'" />
      <appendToFile value="true" />
      <cleaner type="UGTS.Log4NetCleaner.LogCleaner, UGTS.Log4NetCleaner">
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
</configuration>
```

In this example, the only new definitions beyond what RollingFileAppender uses are: the type of the appender, and the cleaner tag with the properties defined under it.  This example will clean out any log files older than 90 days and will remove the oldest log files when the total size of all log files exceeds 100 megabytes.

The properties defined under the cleaner tag include:

- basePath:
        Sets the root directory of the log files for cleaning.
        If this value is omitted, it will be inferred from the File property.
        Sometimes inference does not work if the File is not a directory but also contains part of the file name.
        Use care when setting this property and the FileExtension.  The appender will clean any directory you give it, and with a FileExtension of * this can 
        result in non-log files being removed in the specified directory and subdirectories.

- fileExtension:
        Use this to restrict which files by file extension will be cleaned up.
        If this property is omitted, the file extension will be inferred from the extension of the log files created.  If the file extension cannot be inferred,
        then for safety, no cleaning will be performed.  Usually inference works well enough so that you need not specify this property.
        You can explicitly set this property to * to clean all files whatever the file extension or lack thereof.  Use * at your own risk.
        The lastcleaning.check file is never removed regardless of the value of this property.
        This extension can include or omit a leading dot, it will not affect the results, and the extension is not case sensititve.
        Rolling backup files ending in .ext.N will also be removed along with files ending in .ext
        For example, if this property has value 'txt', then the log files app.txt and app.txt.14 would be removed but not app.log or app.log.14 or app.txt.log.

- maximumDirectorySize: 
        Sets the maximum age of log files (in a decimal number of days) to keep when cleaning the log directory.
        If this value is specified, then log files (from oldest to newest) will be deleted until the total size
        of log files is less than the directory maximum.  This parameter must be an integer optionally suffixed 
        with KB, MB, or GB.  For example, 100MB specifies a maxmimum directory size of 100 megabytes.
        Either this value or MaximumFileAgeDays must be specified or no cleaning will be performed.
        If this value is blank, then cleaning will only be done according to the MaximumFileAgeDays parameter.

- maximumFileAgeDays:
        Sets the maximum allowed size of the log directory in bytes for all log files found.
        Either this value or MaximumDirectorySize must be specified or no cleaning will be performed.
        If this value is blank, then cleaning will only be done according to the MaximumDirectorySize parameter.

- periodMinutes:
        Sets the decimal number of minutes to wait between directory cleaning checks.
        This period defaults to 480 minutes if not specified.  Cleaning is performed at the first logging call where it has
        been at least this many minutes since the last cleaning.  The date of the last cleaning is stored between process runs
        by using the last modified date (UTC) of the lastcleaning.check file which is placed in the basePath directory.
        
- waitType:
        Sets the type of waiting to do when cleaning the log directory.
        This can be either: Never or Always.
        If the value is Always (default), then log directory cleaning will run on the same thread as logging, and will block until cleaning is complete.  This is recommended for batch and other background jobs.
        If the value is Never, then cleaning is performed asynchronously in the background on a different thread.  This is recommended for web and other processes which run continuously.

## Notes: 

- If your configuration is setup to log to files without a file extension, then for safety, the cleaner will NOT attempt to infer a file extension (in order to prevent the accidental deletion of files other than log files).

- You should not need to define the waitType property to Never unless the application does logging on a thread where occasional delays will be noticeable when the period check for old files is performed.  The default waitType is set to Always to handle the case of background processes where logging delays are not critical.  If the waitType is set to Never, and the process exits while in the middle of cleaning up the log directory, then cleaning will be retried (also with a wait type of Never) the next time the process is started.

- It is not recommended to have multiple processes setup to clean the same logging directory with the same cleaning parameters.  If this is configured, all processes will attempt to clean the directory at about the same time.  The first one to start will get the work done, and the rest may silently continue when they attempt to delete files that no longer exist.


## Log4Net Notes:

If you are using Log4Net with .NET Core, the way in which you initialize the Log4Net Repository has changed.  You can no longer simply do this:

```
    XmlConfigurator.Configure();
```

Instead you must do something like this:

```
    var fullPathToYourConfigFile = "some absolute or relative path goes here";
    var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
    var configFile = new FileInfo(fullPathToYourConfigFile);
    XmlConfigurator.Configure(logRepository, configFile);
```

This is because .NET Core does not support app.config files and will therefore not try to load any such config file automatically for you.  Instead you must tell Log4Net where your logging configuration file is.  This file can continue to have the same format as you see in the example above, but all the other sections that would have been present in a typical app.config file are not necessary.
