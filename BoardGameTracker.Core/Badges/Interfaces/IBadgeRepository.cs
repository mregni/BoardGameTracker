using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Badges.Interfaces;

public interface IBadgeRepository : IRepository<Badge>
{
    Task<Dictionary<int, List<Badge>>> GetPlayerBadgesBatchAsync(IEnumerable<int> playerIds);
    Task<bool> AwardBadgeToPlayer(int playerId, int badgeId);
}