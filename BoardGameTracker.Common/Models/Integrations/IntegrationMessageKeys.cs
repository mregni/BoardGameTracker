namespace BoardGameTracker.Common.Models.Integrations;

public static class IntegrationMessageKeys
{
    public const string Ok = "ok";
    public const string NotConfigured = "not-configured";
    public const string Unauthorized = "unauthorized";
    public const string Unreachable = "unreachable";
    public const string Timeout = "timeout";
    public const string RateLimited = "rate-limited";
    public const string Misconfigured = "misconfigured";
    public const string UnexpectedResponse = "unexpected-response";
}
