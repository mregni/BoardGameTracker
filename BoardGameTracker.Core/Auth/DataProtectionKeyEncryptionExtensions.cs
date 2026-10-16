using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public static class DataProtectionKeyEncryptionExtensions
{
    public static IDataProtectionBuilder ProtectKeysWithSecret(this IDataProtectionBuilder builder, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return builder;
        }

        var material = new DataProtectionKeyMaterial(secret);
        builder.Services.AddSingleton(material);
        builder.Services.Configure<KeyManagementOptions>(options => options.XmlEncryptor = new SecretXmlEncryptor(material));
        return builder;
    }

    public static bool WarnWhenKeysAreUnencrypted(this ILogger logger, IServiceProvider services)
    {
        if (services.GetService<DataProtectionKeyMaterial>() != null)
        {
            return false;
        }

        logger.LogWarning(
            "DATA_PROTECTION_KEY is not set: the data protection keys are stored unencrypted in the database, so a copy of the database is enough to read the stored OIDC client secret and integration API keys and to forge password-reset links. Set DATA_PROTECTION_KEY to a random value of at least {MinimumLength} characters (for example the output of 'openssl rand -base64 48') and keep it stable; see the environment variables page of the documentation",
            DataProtectionKeyMaterial.MinimumSecretLength);
        return true;
    }
}
