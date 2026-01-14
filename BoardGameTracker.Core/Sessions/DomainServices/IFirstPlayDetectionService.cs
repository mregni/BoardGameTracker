using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.DomainServices;

public interface IFirstPlayDetectionService
{
    Task<bool> IsFirstPlayAsync(int playerId, int gameId);
    Task<bool> IsFirstPlayAsync(Player player, Game game);
    Task<IEnumerable<int>> GetFirstTimePlayerIdsAsync(int gameId, IEnumerable<int> playerIds);
}
