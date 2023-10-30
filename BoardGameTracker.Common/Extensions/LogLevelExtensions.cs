﻿using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Extensions;

public static class LogLevelExtensions
{
    public static LogLevel GetEnvironmentLogLevel()
    {
        var logLevelString = Environment.GetEnvironmentVariable("LOGLEVEL") ?? "WARNING";
        return logLevelString switch
        {
            "ERROR" => LogLevel.Error,
            "INFO" => LogLevel.Information,
            "DEBUG" => LogLevel.Debug,
            _ => LogLevel.Warning
        };
    }
}