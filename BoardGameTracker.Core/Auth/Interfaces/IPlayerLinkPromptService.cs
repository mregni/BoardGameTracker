using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IPlayerLinkPromptService
{
    Task<PlayerLinkSuggestionsResponse> GetSuggestionsAsync(string userId);
    Task<ProfileResponse> LinkAsync(string userId, int playerId);
    Task<ProfileResponse> CreatePlayerAsync(string userId);
    Task<ProfileResponse> SkipAsync(string userId);
}
