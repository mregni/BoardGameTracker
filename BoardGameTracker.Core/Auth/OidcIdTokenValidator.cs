using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BoardGameTracker.Core.Auth;

public class OidcIdTokenValidator : IOidcIdTokenValidator
{
    public static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(2);
    public static readonly IReadOnlyCollection<string> AllowedAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256];

    private static readonly JsonWebTokenHandler Handler = new();

    private readonly ConditionalWeakTable<OpenIdConnectConfiguration, string> _expiredCertificateWarnings = new();
    private readonly IOidcConfigurationProvider _configurations;
    private readonly ILogger<OidcIdTokenValidator> _logger;

    public OidcIdTokenValidator(IOidcConfigurationProvider configurations, ILogger<OidcIdTokenValidator> logger)
    {
        _configurations = configurations;
        _logger = logger;
    }

    public async Task<OidcIdToken> ValidateAsync(string authority, OpenIdConnectConfiguration configuration, string clientId, string idToken, string expectedNonce)
    {
        var result = await TryValidateAsync(authority, configuration, clientId, idToken);
        if (NeedsFreshConfiguration(result))
        {
            _logger.LogInformation("The id_token of {Authority} failed on its algorithm or signature; refetching the OIDC configuration and signing keys", authority);
            configuration = await _configurations.RefreshAsync(authority);
            result = await TryValidateAsync(authority, configuration, clientId, idToken);
        }

        if (result is null)
        {
            _logger.LogWarning("{Authority} signs id_tokens only with algorithms that BoardGameTracker does not accept, such as HS256; set a signing key at the identity provider so it signs with RS256 or ES256", authority);
            throw Invalid();
        }

        if (!result.IsValid)
        {
            if (result.Exception is SecurityTokenInvalidAlgorithmException)
            {
                _logger.LogWarning("The id_token of {Authority} is signed with an algorithm that BoardGameTracker does not accept; set a signing key at the identity provider so it signs with RS256 or ES256", authority);
            }
            else
            {
                _logger.LogWarning("The id_token of {Authority} was rejected: {Reason}", authority, result.Exception?.GetType().Name ?? "unknown");
            }

            throw Invalid();
        }

        var token = (JsonWebToken)result.SecurityToken;
        EnsureAuthorizedParty(token, clientId, authority);
        EnsureIssuedInThePast(token, authority);
        EnsureNonce(token, expectedNonce, authority);

        var claims = OidcClaims.Parse(Base64UrlEncoder.Decode(token.EncodedPayload));
        if (claims.Subject is not { Length: > 0 } subject)
        {
            _logger.LogWarning("The id_token of {Authority} carried no sub claim", authority);
            throw Invalid();
        }

        return new OidcIdToken(subject, claims);
    }

    private async Task<TokenValidationResult?> TryValidateAsync(string authority, OpenIdConnectConfiguration configuration, string clientId, string idToken)
    {
        return SupportedAlgorithms(configuration).Count == 0
            ? null
            : await Handler.ValidateTokenAsync(idToken, Parameters(authority, configuration, clientId));
    }

    private static bool NeedsFreshConfiguration(TokenValidationResult? result)
    {
        return result is null
            || (!result.IsValid && result.Exception is SecurityTokenInvalidAlgorithmException or SecurityTokenInvalidSignatureException);
    }

    private TokenValidationParameters Parameters(string authority, OpenIdConnectConfiguration configuration, string clientId)
    {
        return new TokenValidationParameters
        {
            ValidIssuer = configuration.Issuer,
            ValidateIssuer = true,
            ValidAudience = clientId,
            ValidateAudience = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyValidator = (key, _, _) => AcceptSigningKey(authority, configuration, key),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = ClockSkew,
            ValidAlgorithms = SupportedAlgorithms(configuration),
        };
    }

    private bool AcceptSigningKey(string authority, OpenIdConnectConfiguration configuration, SecurityKey key)
    {
        if (key is X509SecurityKey { Certificate: { } certificate }
            && certificate.NotAfter.ToUniversalTime() < DateTime.UtcNow
            && _expiredCertificateWarnings.TryAdd(configuration, authority))
        {
            _logger.LogWarning("The signing certificate of {Authority} expired on {ExpiredOn:yyyy-MM-dd}; consider rotating it at the identity provider",
                authority, certificate.NotAfter.ToUniversalTime());
        }

        return true;
    }

    public static List<string> SupportedAlgorithms(OpenIdConnectConfiguration configuration)
    {
        return SupportedAlgorithms(configuration.IdTokenSigningAlgValuesSupported);
    }

    public static List<string> SupportedAlgorithms(ICollection<string> advertised)
    {
        return advertised.Count == 0
            ? AllowedAlgorithms.ToList()
            : AllowedAlgorithms.Where(advertised.Contains).ToList();
    }

    private void EnsureAuthorizedParty(JsonWebToken token, string clientId, string authority)
    {
        var audiences = token.Audiences.ToList();
        var hasParty = token.TryGetPayloadValue<string>(JwtRegisteredClaimNames.Azp, out var authorizedParty);
        if ((audiences.Count > 1 || hasParty) && !string.Equals(authorizedParty, clientId, StringComparison.Ordinal))
        {
            _logger.LogWarning("The id_token of {Authority} was issued to another authorized party", authority);
            throw Invalid();
        }
    }

    private void EnsureIssuedInThePast(JsonWebToken token, string authority)
    {
        if (!token.TryGetPayloadValue<long>(JwtRegisteredClaimNames.Iat, out _)
            || token.IssuedAt > DateTime.UtcNow.Add(ClockSkew))
        {
            _logger.LogWarning("The id_token of {Authority} has a missing or future iat claim", authority);
            throw Invalid();
        }
    }

    private void EnsureNonce(JsonWebToken token, string expectedNonce, string authority)
    {
        var hasNonce = token.TryGetPayloadValue<string>(JwtRegisteredClaimNames.Nonce, out var nonce);
        if (!hasNonce
            || string.IsNullOrEmpty(nonce)
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(nonce), Encoding.UTF8.GetBytes(expectedNonce)))
        {
            _logger.LogWarning("The id_token of {Authority} did not carry the nonce of this sign-in", authority);
            throw Invalid();
        }
    }

    private static DomainException Invalid() => new(Constants.Errors.OidcIdTokenInvalid);
}
