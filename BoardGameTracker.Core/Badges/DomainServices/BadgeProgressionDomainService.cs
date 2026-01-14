using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Badges.Policies;

namespace BoardGameTracker.Core.Badges.DomainServices;

public class BadgeProgressionDomainService : IBadgeProgressionDomainService
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IBadgeLevelProgressionPolicy _progressionPolicy;

    public BadgeProgressionDomainService(
        IBadgeRepository badgeRepository,
        IBadgeLevelProgressionPolicy progressionPolicy)
    {
        _badgeRepository = badgeRepository;
        _progressionPolicy = progressionPolicy;
    }

    public async Task<Badge?> GetNextAvailableBadgeAsync(Player player, BadgeType badgeType)
    {
        var badgesOfType = await GetBadgeProgressionAsync(badgeType);
        var playerBadges = player.Badges.Where(b => b.Type == badgeType).ToList();

        // Get the highest level badge the player has for this type
        var highestLevel = playerBadges
            .Where(b => b.Level.HasValue)
            .Select(b => b.Level!.Value)
            .OrderByDescending(level => _progressionPolicy.GetLevelOrder(level))
            .FirstOrDefault();

        // Find the next badge in progression
        foreach (var badge in badgesOfType.OrderBy(b => _progressionPolicy.GetLevelOrder(b.Level ?? BadgeLevel.Green)))
        {
            if (!badge.Level.HasValue)
                continue;

            if (highestLevel == default)
            {
                // Player has no badges of this type, return the first (Green)
                if (_progressionPolicy.IsStartingLevel(badge.Level.Value))
                    return badge;
            }
            else if (CanAwardBadge(highestLevel, badge.Level.Value))
            {
                return badge;
            }
        }

        return null; // Player has achieved the highest level
    }

    public bool CanAwardBadge(BadgeLevel? currentLevel, BadgeLevel nextLevel)
    {
        if (!currentLevel.HasValue)
            return _progressionPolicy.IsStartingLevel(nextLevel);

        // Use policy to determine if progression is valid
        return _progressionPolicy.CanProgressTo(currentLevel.Value, nextLevel);
    }

    public async Task<IEnumerable<Badge>> GetBadgeProgressionAsync(BadgeType badgeType)
    {
        var allBadges = await _badgeRepository.GetAllAsync();
        return allBadges
            .Where(b => b.Type == badgeType && b.Level.HasValue)
            .OrderBy(b => _progressionPolicy.GetLevelOrder(b.Level!.Value));
    }
}
