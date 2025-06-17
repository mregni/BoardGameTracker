using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges;

public class BadgeService : IBadgeService
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly Dictionary<BadgeType, IBadgeEvaluator> _evaluators;
    
    public BadgeService(IBadgeRepository badgeRepository, IEnumerable<IBadgeEvaluator> evaluators)
    {
        _badgeRepository = badgeRepository;
        _evaluators = evaluators.ToDictionary(x => x.BadgeType, x => x);
    }

    public async Task AwardBadgesAsync(Session session)
    {
        foreach (var player in session.PlayerSessions)
        {
            var badges = await _badgeRepository.GetAllAsync();
            var playerBadges = await _badgeRepository.GetPlayerBadgesAsync(player.PlayerId);

            var newBadges = badges
                .Where(x => playerBadges.All(y => y.Id != x.Id));

            foreach (var newBadge in newBadges)
            {
                if (!_evaluators.TryGetValue(newBadge.Type, out var evaluator))
                {
                    continue;
                }
            
                if (await evaluator.CanAwardBadge(player.PlayerId, newBadge, session))
                {
                    await _badgeRepository.AwardBatchToPlayer(player.PlayerId, newBadge.Id);
                }
            }
        }
    }
}