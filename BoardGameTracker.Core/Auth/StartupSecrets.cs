namespace BoardGameTracker.Core.Auth;

public static class StartupSecrets
{
    public const int MinimumJwtSecretLength = 32;
    public const string AuthDisabledJwtSecret = "auth-disabled-placeholder-key-not-used";
    private const string PlaceholderPrefix = "CHANGEME";

    public static bool IsPlaceholder(string? value) =>
        value != null && value.Trim().StartsWith(PlaceholderPrefix, StringComparison.OrdinalIgnoreCase);

    public static string PlaceholderMessage(string variable) =>
        $"{variable} still holds the CHANGEME placeholder from the example configuration. Replace it with your own random value of at least 32 characters (for example the output of `openssl rand -base64 48`).";

    public static string? FindProblem(string? jwtSecret, string? dataProtectionKey, bool authEnabled)
    {
        try
        {
            ResolveJwtSecret(jwtSecret, authEnabled);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }

        return string.IsNullOrWhiteSpace(dataProtectionKey) ? null : DataProtectionKeyProblem(dataProtectionKey);
    }

    public static string? DataProtectionKeyProblem(string secret)
    {
        if (IsPlaceholder(secret))
        {
            return PlaceholderMessage("DATA_PROTECTION_KEY");
        }

        return secret.Length < DataProtectionKeyMaterial.MinimumSecretLength
            ? $"DATA_PROTECTION_KEY must be at least {DataProtectionKeyMaterial.MinimumSecretLength} characters long, but was {secret.Length}."
            : null;
    }

    public static string ResolveJwtSecret(string? secret, bool authEnabled)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            if (authEnabled)
            {
                throw new InvalidOperationException(
                    "JWT_SECRET is not set. Set it to at least 32 random characters (for example the output of `openssl rand -base64 48`), or set AUTH_ENABLED=false to run without accounts. See https://mregni.github.io/BoardGameTracker/getting-started/environment-variables/");
            }

            return AuthDisabledJwtSecret;
        }

        if (!authEnabled)
        {
            return secret;
        }

        if (IsPlaceholder(secret))
        {
            throw new InvalidOperationException(PlaceholderMessage("JWT_SECRET"));
        }

        if (secret.Length < MinimumJwtSecretLength)
        {
            throw new InvalidOperationException(
                $"JWT_SECRET must be at least {MinimumJwtSecretLength} characters long, but was {secret.Length}. Generate one with `openssl rand -base64 48`.");
        }

        return secret;
    }
}
