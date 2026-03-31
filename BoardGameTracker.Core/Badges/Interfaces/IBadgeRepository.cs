using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Badges.Interfaces;

public interface IBadgeRepository : ICrudHelper<Badge>
{
    Task<List<Badge>> GetPlayerBadgesAsync(int playerId);
    Task<Dictionary<int, List<Badge>>> GetPlayerBadgesBatchAsync(IEnumerable<int> playerIds);
    Task<bool> AwardBatchToPlayer(int playerId, int badgeId);
}