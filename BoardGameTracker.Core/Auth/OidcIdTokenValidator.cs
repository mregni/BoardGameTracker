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

    private readonly IOidcConfigurationProvider _configurations;
    private readonly ILogger<OidcIdTokenValidator> _logger;

    public OidcIdTokenValidator(IOidcConfigurationProvider configurations, ILogger<OidcIdTokenValidator> logger)
    {
        _configurations = configurations;
        _logger = logger;
    }

    public async Task<OidcIdToken> ValidateAsync(string authority, OpenIdConnectConfiguration configuration, string clientId, string idToken, string expectedNonce)
    {
        if (SupportedAlgorithms(configuration).Count == 0)
        {
            _logger.LogWarning("{Authority} signs id_tokens only with algorithms outside the allow list", authority);
            throw Invalid();
        }

        var result = await Handler.ValidateTokenAsync(idToken, Parameters(configuration, clientId));
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException && HasUnknownKeyId(idToken, configuration))
        {
            _logger.LogInformation("The id_token of {Authority} is signed with an unknown key; refetching the signing keys", authority);
            var refreshed = await _configurations.RefreshAsync(authority);
            result = await Handler.ValidateTokenAsync(idToken, Parameters(refreshed, clientId));
        }

        if (!result.IsValid)
        {
            _logger.LogWarning("The id_token of {Authority} was rejected: {Reason}", authority, result.Exception?.GetType().Name ?? "unknown");
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

    private static bool HasUnknownKeyId(string idToken, OpenIdConnectConfiguration configuration)
    {
        if (!Handler.CanReadToken(idToken))
        {
            return false;
        }

        var keyId = Handler.ReadJsonWebToken(idToken).Kid;
        return !string.IsNullOrEmpty(keyId) && configuration.SigningKeys.All(k => k.KeyId != keyId);
    }

    private static TokenValidationParameters Parameters(OpenIdConnectConfiguration configuration, string clientId)
    {
        return new TokenValidationParameters
        {
            ValidIssuer = configuration.Issuer,
            ValidateIssuer = true,
            ValidAudience = clientId,
            ValidateAudience = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = ClockSkew,
            ValidAlgorithms = SupportedAlgorithms(configuration),
        };
    }

    public static List<string> SupportedAlgorithms(OpenIdConnectConfiguration configuration)
    {
        var advertised = configuration.IdTokenSigningAlgValuesSupported;
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
