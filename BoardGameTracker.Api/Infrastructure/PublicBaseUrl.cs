using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Api.Infrastructure;

public static class PublicBaseUrl
{
    public static async Task<(string BaseUrl, bool Configured)> ResolveAsync(IPublicUrlBuilder publicUrlBuilder, HttpRequest request)
    {
        var configured = await publicUrlBuilder.GetConfiguredBaseUrlAsync();
        return configured != null ? (configured, true) : ($"{request.Scheme}://{request.Host}", false);
    }
}
