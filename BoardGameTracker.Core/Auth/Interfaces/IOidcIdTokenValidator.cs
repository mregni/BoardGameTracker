using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcIdTokenValidator
{
    Task<OidcIdToken> ValidateAsync(string authority, OpenIdConnectConfiguration configuration, string clientId, string idToken, string expectedNonce);
}

public record OidcIdToken(string Subject, OidcClaims Claims);
