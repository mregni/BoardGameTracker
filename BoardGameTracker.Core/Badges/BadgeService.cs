using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Badges;

public class BadgeService : IBadgeService
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly Dictionary<BadgeType, IBadgeEvaluator> _evaluators;
    
    public BadgeService(IBadgeRepository badgeRepository, ISessionRepository sessionRepository, IEnumerable<IBadgeEvaluator> evaluators)
    {
        _badgeRepository = badgeRepository;
        _sessionRepository = sessionRepository;
        _evaluators = evaluators.ToDictionary(x => x.BadgeType, x => x);
    }

    public async Task AwardBadgesAsync(Session session)
    {
        foreach (var player in session.PlayerSessions)
        {
            var badges = await _badgeRepository.GetAllAsync();
            var playerBadges = await _badgeRepository.GetPlayerBadgesAsync(player.PlayerId);

            var newBadges = badges
                .Where(x => playerBadges.All(y => y.Id != x.Id))
                .GroupBy(x => x.Type);

            var sessions = await _sessionRepository.GetByPlayer(player.PlayerId);
            foreach (var badgeGroup in newBadges)
            {
                await ProcessBadgeGroup(badgeGroup, session, player, sessions);
            }
        }
    }

    private async Task ProcessBadgeGroup(IGrouping<BadgeType, Badge> badgeGroup, Session session, PlayerSession player, List<Session> sessions)
    {
        foreach (var newBadge in badgeGroup.OrderBy(x => x.Level))
        {
            if (!_evaluators.TryGetValue(newBadge.Type, out var evaluator))
            {
                return;
            }
            
            if (await evaluator.CanAwardBadge(player.PlayerId, newBadge, session, sessions))
            {
                await _badgeRepository.AwardBatchToPlayer(player.PlayerId, newBadge.Id);
            }
            else
            {
                return;
            }
        }
    }
}