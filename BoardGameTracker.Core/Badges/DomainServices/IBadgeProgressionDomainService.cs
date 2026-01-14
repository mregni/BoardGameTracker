using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Badges.DomainServices;

public interface IBadgeProgressionDomainService
{
    Task<Badge?> GetNextAvailableBadgeAsync(Player player, BadgeType badgeType);
    bool CanAwardBadge(BadgeLevel? currentLevel, BadgeLevel nextLevel);
    Task<IEnumerable<Badge>> GetBadgeProgressionAsync(BadgeType badgeType);
}
