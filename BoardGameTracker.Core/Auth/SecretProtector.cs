using System.Security.Cryptography;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class SecretProtector : ISecretProtector
{
    private const string Purpose = "BoardGameTracker.Secrets.v1";
    private const string Prefix = "dp1:";

    private readonly IDataProtector _protector;
    private readonly ILogger<SecretProtector> _logger;

    public SecretProtector(IDataProtectionProvider provider, ILogger<SecretProtector> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Protect(string plaintext)
    {
        return Prefix + _protector.Protect(plaintext);
    }

    public string Unprotect(string stored)
    {
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return stored;
        }

        try
        {
            return _protector.Unprotect(stored[Prefix.Length..]);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "A stored secret could not be decrypted; the data-protection keys may have been lost. Re-enter the secret in the settings");
            throw;
        }
    }
}
