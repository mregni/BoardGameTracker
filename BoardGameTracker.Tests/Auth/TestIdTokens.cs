using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BoardGameTracker.Tests.Auth;

public sealed class TestIdTokens : IDisposable
{
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };
    private readonly RSA _rsa = RSA.Create(2048);

    public TestIdTokens(string keyId = "key-1")
    {
        KeyId = keyId;
        PublicKey = new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = keyId };
    }

    public string KeyId { get; }

    public RsaSecurityKey PublicKey { get; }

    public string JwksJson
    {
        get
        {
            var parameters = _rsa.ExportParameters(false);
            return JsonSerializer.Serialize(new
            {
                keys = new[]
                {
                    new
                    {
                        kty = "RSA",
                        use = "sig",
                        alg = "RS256",
                        kid = KeyId,
                        n = Base64UrlEncoder.Encode(parameters.Modulus),
                        e = Base64UrlEncoder.Encode(parameters.Exponent),
                    },
                },
            });
        }
    }

    public X509Certificate2 Certificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var request = new CertificateRequest("CN=authentik Self-signed Certificate", _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var withKey = request.CreateSelfSigned(notBefore, notAfter);
        return X509CertificateLoader.LoadCertificate(withKey.RawData);
    }

    public string JwksJsonWithCertificate(X509Certificate2 certificate, string? alg = "RS256", string? use = "sig", string? x5c = null)
    {
        var parameters = _rsa.ExportParameters(false);
        return JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use,
                    alg,
                    kid = KeyId,
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent),
                    x5c = new[] { x5c ?? Convert.ToBase64String(certificate.RawData) },
                },
            },
        });
    }

    public static Dictionary<string, object> Claims(string issuer, string audience, string subject, string nonce)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new Dictionary<string, object>
        {
            ["iss"] = issuer,
            ["aud"] = audience,
            ["sub"] = subject,
            ["nonce"] = nonce,
            ["iat"] = now,
            ["nbf"] = now,
            ["exp"] = now + 300,
        };
    }

    public string Sign(IDictionary<string, object> claims, string algorithm = SecurityAlgorithms.RsaSha256, string? keyId = null)
    {
        var key = new RsaSecurityKey(_rsa) { KeyId = keyId ?? KeyId };
        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            Claims = claims,
            SigningCredentials = new SigningCredentials(key, algorithm),
        });
    }

    public static string SignWithSymmetricKey(IDictionary<string, object> claims)
    {
        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = "hmac" };
        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            Claims = claims,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });
    }

    public void Dispose()
    {
        _rsa.Dispose();
    }
}
