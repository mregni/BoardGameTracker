using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Configuration.Interfaces;
using Serilog.Events;

namespace BoardGameTracker.Core.Configuration;

public class EnvironmentProvider : IEnvironmentProvider
{
    public string EnvironmentName => EnvironmentExtensions.GetEnvironmentName();

    public int Port => int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var parsedPort) && parsedPort >= 0
        ? parsedPort
        : 7178;

    public bool StatisticsEnabled =>
        bool.TryParse(Environment.GetEnvironmentVariable("STATISTICS_ENABLED"), out var statisticsEnabled) && statisticsEnabled;

    public bool RagEnabled =>
        bool.TryParse(Environment.GetEnvironmentVariable("RAG_ENABLED"), out var ragEnabled) && ragEnabled;

    public LogEventLevel LogLevel => LogLevelExtensions.GetEnvironmentLogLevel();
    public bool IsDevelopment => EnvironmentName.Equals("development", StringComparison.OrdinalIgnoreCase);

    public bool AuthEnabled =>
        !string.Equals(Environment.GetEnvironmentVariable("AUTH_ENABLED"), "false", StringComparison.OrdinalIgnoreCase);

    public string? JwtSecret
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("JWT_SECRET");
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    public string? AdminPassword => Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

    public IReadOnlyList<string> TrustedProxies => SplitList(Environment.GetEnvironmentVariable("TRUSTED_PROXIES"));

    public IReadOnlyList<string> CorsOrigins => SplitList(Environment.GetEnvironmentVariable("CORS_ORIGINS"));

    public bool SwaggerEnabled =>
        bool.TryParse(Environment.GetEnvironmentVariable("SWAGGER_ENABLED"), out var swaggerEnabled)
            ? swaggerEnabled
            : IsDevelopment;

    private static IReadOnlyList<string> SplitList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string? SmtpHost => Environment.GetEnvironmentVariable("SMTP_HOST");

    public int SmtpPort => int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) && port > 0
        ? port
        : 587;

    public string? SmtpUsername => Environment.GetEnvironmentVariable("SMTP_USERNAME");

    public string? SmtpPassword => Environment.GetEnvironmentVariable("SMTP_PASSWORD");

    public bool SmtpUseSsl =>
        !string.Equals(Environment.GetEnvironmentVariable("SMTP_USE_SSL"), "false", StringComparison.OrdinalIgnoreCase);

    public string? SmtpFromAddress => Environment.GetEnvironmentVariable("SMTP_FROM_ADDRESS");

    public string? SmtpFromName => Environment.GetEnvironmentVariable("SMTP_FROM_NAME");

    public bool EmailEnabled => !string.IsNullOrWhiteSpace(SmtpHost) && !string.IsNullOrWhiteSpace(SmtpFromAddress);
}