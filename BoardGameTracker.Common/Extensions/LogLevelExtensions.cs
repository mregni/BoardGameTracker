using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Common.Extensions;

public static class LogLevelExtensions
{
    public static LogLevel GetEnvironmentLogLevel()
    {
        var logLevelString = Environment.GetEnvironmentVariable("LOGLEVEL") ?? "WARNING";
        logLevelString = logLevelString.ToUpper().Trim();
        return logLevelString switch
        {
            "ERROR" => LogLevel.Error,
            "INFO" => LogLevel.Information,
            "DEBUG" => LogLevel.Debug,
            _ => LogLevel.Warning
        };
    }
}