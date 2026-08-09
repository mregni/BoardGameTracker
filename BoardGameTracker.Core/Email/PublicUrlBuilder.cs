using BoardGameTracker.Common;
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
        var baseUrl = await GetBaseUrlAsync();
        return $"{baseUrl}/rsvp?linkId={linkId}";
    }

    public async Task<string> BuildResetUrlAsync(string userId, string token)
    {
        var baseUrl = await GetBaseUrlAsync();
        return $"{baseUrl}/reset-password?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }

    private async Task<string> GetBaseUrlAsync()
    {
        var url = await _configRepository.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl);
        return (url ?? string.Empty).TrimEnd('/');
    }
}
