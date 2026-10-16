using System.Security.Cryptography;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Settings;

public class IntegrationKeyProtector : IIntegrationKeyProtector
{
    private const string Purpose = "BoardGameTracker.IntegrationKeys.v1";
    private const string Prefix = "dp1:";

    private readonly IDataProtector _protector;
    private readonly ILogger<IntegrationKeyProtector> _logger;

    public IntegrationKeyProtector(IDataProtectionProvider provider, ILogger<IntegrationKeyProtector> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Protect(string plaintext)
    {
        return Prefix + _protector.Protect(plaintext);
    }

    public bool IsProtected(string stored)
    {
        return stored.StartsWith(Prefix, StringComparison.Ordinal);
    }

    public string? Reveal(string setting, string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        if (!IsProtected(stored))
        {
            return stored;
        }

        try
        {
            return _protector.Unprotect(stored[Prefix.Length..]);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            _logger.LogWarning("The stored {Setting} could not be decrypted; the data-protection keys may have been lost or rotated. The integration is treated as not configured until the key is entered again in the settings", setting);
            return null;
        }
    }
}
