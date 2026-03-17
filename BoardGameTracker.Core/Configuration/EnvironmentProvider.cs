using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Configuration.Interfaces;
using Serilog.Events;

namespace BoardGameTracker.Core.Configuration;

public class EnvironmentProvider : IEnvironmentProvider
{
    public string EnvironmentName =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("ENVIRONMENT")
        ?? "development";

    public int Port => int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var parsedPort) && parsedPort >= 0
        ? parsedPort
        : 7178;

    public bool StatisticsEnabled =>
        bool.TryParse(Environment.GetEnvironmentVariable("STATISTICS_ENABLED"), out var statisticsEnabled) && statisticsEnabled;

    public LogEventLevel LogLevel => LogLevelExtensions.GetEnvironmentLogLevel();
    public bool IsDevelopment => EnvironmentName.Equals("development", StringComparison.OrdinalIgnoreCase);
}