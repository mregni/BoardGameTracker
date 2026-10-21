using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerAccountImageService
{
    Task<bool> ApplyAccountImageAsync(ApplicationUser user);
    Task ReleaseAsync(int? playerId);
    Task RelinkAsync(int? previousPlayerId, ApplicationUser user);
    Task<PlayerLinkedAccountDto?> FindLinkedAccountAsync(int playerId);
    Task<string?> CopyLinkedAccountImageAsync(int playerId);
    Task<int> BackfillAsync();
}
