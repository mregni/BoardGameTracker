namespace BoardGameTracker.Common.Extensions;

public static class EnvironmentExtensions
{
    public static string GetEnvironmentName() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("ENVIRONMENT")
        ?? "production";

    public static bool IsDevelopment() =>
        GetEnvironmentName().Equals("development", StringComparison.OrdinalIgnoreCase);
}
