using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;

namespace BoardGameTracker.Core.Email;

public class PublicUrlBuilder : IPublicUrlBuilder
{
    private readonly IConfigRepository _configRepository;

    public PublicUrlBuilder(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    public async Task<string> BuildRsvpUrlAsync(Guid linkId)
    {
        var baseUrl = await RequireBaseUrlAsync();
        return $"{baseUrl}/rsvp?linkId={linkId}";
    }

    public async Task<string> BuildResetUrlAsync(string userId, string token)
    {
        var baseUrl = await RequireBaseUrlAsync();
        return $"{baseUrl}/reset-password?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }

    public async Task<string?> GetConfiguredBaseUrlAsync()
    {
        var url = await _configRepository.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl);
        return Normalize(url);
    }

    public static string? Normalize(string? url)
    {
        var trimmed = (url ?? string.Empty).Trim().TrimEnd('/');
        return trimmed.Length == 0 ? null : trimmed;
    }

    private async Task<string> RequireBaseUrlAsync()
    {
        return await GetConfiguredBaseUrlAsync() ?? throw new DomainException(Constants.Errors.PublicUrlNotConfigured);
    }
}
