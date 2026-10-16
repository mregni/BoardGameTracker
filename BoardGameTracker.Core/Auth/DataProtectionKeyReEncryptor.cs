using System.Security.Cryptography;
using System.Xml;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class DataProtectionKeyReEncryptor : IDataProtectionKeyReEncryptor
{
    private readonly MainDbContext _context;
    private readonly ILogger<DataProtectionKeyReEncryptor> _logger;
    private readonly DataProtectionKeyMaterial? _material;

    public DataProtectionKeyReEncryptor(
        MainDbContext context,
        ILogger<DataProtectionKeyReEncryptor> logger,
        DataProtectionKeyMaterial? material = null)
    {
        _context = context;
        _logger = logger;
        _material = material;
    }

    public async Task<int> EncryptPlaintextKeysAsync()
    {
        if (_material == null)
        {
            return 0;
        }

        var keys = await _context.DataProtectionKeys.ToListAsync();
        var encrypted = 0;
        foreach (var key in keys.Where(key => !string.IsNullOrEmpty(key.Xml)))
        {
            try
            {
                var xml = DataProtectionKeyXml.EncryptPlaintextSecrets(key.Xml!, _material);
                if (xml == null)
                {
                    continue;
                }

                key.Xml = xml;
                encrypted++;
            }
            catch (Exception ex) when (ex is CryptographicException or XmlException)
            {
                _logger.LogWarning(ex, "Data protection key {FriendlyName} was left unencrypted because it could not be re-encrypted", key.FriendlyName);
            }
        }

        if (encrypted > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Encrypted {Count} existing data protection keys with DATA_PROTECTION_KEY", encrypted);
        }

        return encrypted;
    }
}
