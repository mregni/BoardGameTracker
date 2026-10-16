using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Integrations;

public sealed record IntegrationTestResult(IntegrationConnectionStatus Status, string MessageKey)
{
    public static readonly IntegrationTestResult Ok = new(IntegrationConnectionStatus.Ok, IntegrationMessageKeys.Ok);

    public static readonly IntegrationTestResult NotConfigured =
        new(IntegrationConnectionStatus.NotConfigured, IntegrationMessageKeys.NotConfigured);

    public static readonly IntegrationTestResult Unauthorized =
        new(IntegrationConnectionStatus.Unauthorized, IntegrationMessageKeys.Unauthorized);

    public static readonly IntegrationTestResult RateLimited =
        new(IntegrationConnectionStatus.RateLimited, IntegrationMessageKeys.RateLimited);

    public static IntegrationTestResult Unreachable(string messageKey = IntegrationMessageKeys.Unreachable) =>
        new(IntegrationConnectionStatus.Unreachable, messageKey);
}
