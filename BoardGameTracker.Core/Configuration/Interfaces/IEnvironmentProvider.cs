using Serilog.Events;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IEnvironmentProvider
{
    string EnvironmentName { get;  }
    int Port { get; }
    bool StatisticsEnabled { get; }
    bool RagEnabled { get; }
    LogEventLevel LogLevel { get; }
    bool IsDevelopment { get; }
    bool AuthEnabled { get; }
    string? JwtSecret { get; }
    string? AdminPassword { get; }
    IReadOnlyList<string> TrustedProxies { get; }
    IReadOnlyList<string> CorsOrigins { get; }
    bool SwaggerEnabled { get; }
    string? SmtpHost { get; }
    int SmtpPort { get; }
    string? SmtpUsername { get; }
    string? SmtpPassword { get; }
    bool SmtpUseSsl { get; }
    string? SmtpFromAddress { get; }
    string? SmtpFromName { get; }
    bool EmailEnabled { get; }
}