using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Badges;

public class BadgeService : IBadgeService
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly Dictionary<BadgeType, IBadgeEvaluator> _evaluators;
    private readonly ILogger<BadgeService> _logger;

    public BadgeService(IBadgeRepository badgeRepository, ISessionRepository sessionRepository, IEnumerable<IBadgeEvaluator> evaluators, ILogger<BadgeService> logger)
    {
        _badgeRepository = badgeRepository;
        _sessionRepository = sessionRepository;
        _evaluators = evaluators.ToDictionary(x => x.BadgeType, x => x);
        _logger = logger;
    }

    public async Task AwardBadgesAsync(Session session)
    {
        _logger.LogDebug("Evaluating badges for session {SessionId}", session.Id);
        var playerIds = session.PlayerSessions.Select(ps => ps.PlayerId).Distinct().ToList();

        if (playerIds.Count == 0)
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
        _logger.LogDebug("Fetching all badges");
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
                _logger.LogInformation("Awarding badge {BadgeId} ({BadgeType}) to player {PlayerId}", newBadge.Id, newBadge.Type, player.PlayerId);
                await _badgeRepository.AwardBatchToPlayer(player.PlayerId, newBadge.Id);
            }
            else
            {
                return;
            }
        }
    }
}