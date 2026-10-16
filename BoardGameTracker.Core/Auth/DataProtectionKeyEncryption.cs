using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameTracker.Core.Auth;

public sealed class DataProtectionKeyMaterial
{
    public const int MinimumSecretLength = 32;
    private static readonly byte[] DerivationInfo = Encoding.UTF8.GetBytes("BoardGameTracker.DataProtectionKeys.v1");

    public DataProtectionKeyMaterial(string secret)
    {
        if (secret.Length < MinimumSecretLength)
        {
            throw new ArgumentException(
                $"DATA_PROTECTION_KEY must be at least {MinimumSecretLength} characters long, but was {secret.Length}.", nameof(secret));
        }

        Key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secret), 32, info: DerivationInfo);
    }

    public byte[] Key { get; }
}

public sealed class SecretXmlEncryptor : IXmlEncryptor
{
    internal const string ElementName = "encryptedKey";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly DataProtectionKeyMaterial _material;

    public SecretXmlEncryptor(DataProtectionKeyMaterial material)
    {
        _material = material;
    }

    public EncryptedXmlInfo Encrypt(XElement plaintextElement)
    {
        var plaintext = Encoding.UTF8.GetBytes(plaintextElement.ToString(SaveOptions.DisableFormatting));
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];

        using (var aes = new AesGcm(_material.Key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        ciphertext.CopyTo(payload, NonceSize + TagSize);

        var element = new XElement(ElementName, new XElement("value", Convert.ToBase64String(payload)));
        return new EncryptedXmlInfo(element, typeof(SecretXmlDecryptor));
    }

    internal static XElement Decrypt(XElement encryptedElement, byte[] key)
    {
        var payload = Convert.FromBase64String((string?)encryptedElement.Element("value") ?? string.Empty);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("The encrypted data protection key is malformed.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return XElement.Parse(Encoding.UTF8.GetString(plaintext));
    }
}

public sealed class SecretXmlDecryptor : IXmlDecryptor
{
    private readonly DataProtectionKeyMaterial? _material;

    public SecretXmlDecryptor(IServiceProvider services)
    {
        _material = services.GetService<DataProtectionKeyMaterial>();
    }

    public XElement Decrypt(XElement encryptedElement)
    {
        if (_material == null)
        {
            throw new CryptographicException(
                "A data protection key is encrypted, but DATA_PROTECTION_KEY is not set. Restore the variable with its original value.");
        }

        return SecretXmlEncryptor.Decrypt(encryptedElement, _material.Key);
    }
}
