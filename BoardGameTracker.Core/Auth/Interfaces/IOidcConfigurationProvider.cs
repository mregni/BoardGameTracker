using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcConfigurationProvider
{
    Task<OpenIdConnectConfiguration> GetAsync(string authority, CancellationToken cancellationToken = default);
    Task<OpenIdConnectConfiguration> RefreshAsync(string authority, CancellationToken cancellationToken = default);
    void Invalidate(string authority);
}
