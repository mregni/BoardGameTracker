using System.Globalization;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Configuration;

public class EnvironmentProvider : IEnvironmentProvider
{
    public string EnvironmentName => Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "development";

    public int Port => int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var parsedPort) && parsedPort >= 0
        ? parsedPort
        : 7178;

    public bool EnableStatistics =>
        bool.TryParse(Environment.GetEnvironmentVariable("STATISTICS"), out var enableLogging) && enableLogging;

    public LogLevel LogLevel => LogLevelExtensions.GetEnvironmentLogLevel();
    public bool IsDevelopment => EnvironmentName == "development";
}