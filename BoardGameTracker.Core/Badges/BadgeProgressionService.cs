using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Badges;

public class BadgeProgressionService : IBadgeProgressionService
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IBadgeLevelProgressionPolicy _progressionPolicy;
    private readonly ILogger<BadgeProgressionService> _logger;

    public BadgeProgressionService(
        IBadgeRepository badgeRepository,
        IBadgeLevelProgressionPolicy progressionPolicy,
        ILogger<BadgeProgressionService> logger)
    {
        _badgeRepository = badgeRepository;
        _progressionPolicy = progressionPolicy;
        _logger = logger;
    }

    public async Task<Badge?> GetNextAvailableBadgeAsync(Player player, BadgeType badgeType)
    {
        _logger.LogDebug("Getting next available badge of type {BadgeType} for player {PlayerId}", badgeType, player.Id);
        var badgesOfType = await GetBadgeProgressionAsync(badgeType);
        var playerBadges = player.Badges.Where(b => b.Type == badgeType).ToList();

        var highestLevel = playerBadges
            .Where(b => b.Level.HasValue)
            .Select(b => b.Level!.Value)
            .OrderByDescending(level => _progressionPolicy.GetLevelOrder(level))
            .FirstOrDefault();

        foreach (var badge in badgesOfType.OrderBy(b => _progressionPolicy.GetLevelOrder(b.Level ?? BadgeLevel.Green)))
        {
            if (!badge.Level.HasValue)
                continue;

            if (highestLevel == default)
            {
                if (_progressionPolicy.IsStartingLevel(badge.Level.Value))
                    return badge;
            }
            else if (CanAwardBadge(highestLevel, badge.Level.Value))
            {
                return badge;
            }
        }

        return null;
    }

    public bool CanAwardBadge(BadgeLevel? currentLevel, BadgeLevel nextLevel)
    {
        if (!currentLevel.HasValue)
            return _progressionPolicy.IsStartingLevel(nextLevel);

        return _progressionPolicy.CanProgressTo(currentLevel.Value, nextLevel);
    }

    public async Task<IEnumerable<Badge>> GetBadgeProgressionAsync(BadgeType badgeType)
    {
        _logger.LogDebug("Calculating badge progression for type {BadgeType}", badgeType);
        var allBadges = await _badgeRepository.GetAllAsync();
        return allBadges
            .Where(b => b.Type == badgeType && b.Level.HasValue)
            .OrderBy(b => _progressionPolicy.GetLevelOrder(b.Level!.Value));
    }
}
