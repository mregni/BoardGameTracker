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
        var playerIds = session.PlayerSessions.Select(ps => ps.PlayerId).Distinct().ToList();

        if (!playerIds.Any())
        {
            return;
        }

        var allBadges = await _badgeRepository.GetAllAsync();
        var playerBadgesMapData = await _badgeRepository.GetPlayerBadgesBatchAsync(playerIds);
        var playerSessionsMap
            = await _sessionRepository.GetByPlayerBatchAsync(playerIds);

        var playerBadgesMap = playerBadgesMapData
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(b => b.Id).ToHashSet());

        foreach (var player in session.PlayerSessions)
        {
            var earnedBadgeIds = playerBadgesMap[player.PlayerId];

            var newBadges = allBadges
                .Where(x => !earnedBadgeIds.Contains(x.Id))
                .GroupBy(x => x.Type);

            var playerSessions = playerSessionsMap[player.PlayerId];

            foreach (var badgeGroup in newBadges)
            {
                await ProcessBadgeGroup(badgeGroup, session, player, playerSessions);
            }
        }
    }

    public Task<List<Badge>> GetAllBadgesAsync()
    {
        return _badgeRepository.GetAllAsync();
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