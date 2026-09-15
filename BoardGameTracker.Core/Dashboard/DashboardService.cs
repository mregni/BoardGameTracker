using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IGameRepository gameRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        IPlayerRepository playerRepository,
        ISessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<DashboardService> logger)
    {
        _gameRepository = gameRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _playerRepository = playerRepository;
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<DashboardStatisticsDto> GetStatistics(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calculating dashboard statistics");
        var totalGames = await _gameRepository.CountAsync(cancellationToken);
        var activePlayers = await _playerRepository.CountAsync(cancellationToken);
        var sessionsPlayed = await _sessionRepository.CountAsync(cancellationToken);
        var totalPlayedTime = await _sessionRepository.GetTotalPlayTime(cancellationToken);
        var totalCollectionValue = await _gameStatisticsRepository.GetTotalPayedAsync(cancellationToken);
        var avgGamePrice = await _gameStatisticsRepository.GetMeanPayedAsync(cancellationToken);
        var expansionsOwned = await _gameRepository.GetTotalExpansionCount(cancellationToken);
        var avgSessionTime = await _sessionRepository.GetMeanPlayTime(cancellationToken);

        var recentSessions = await _sessionRepository.GetRecentSessions(4, cancellationToken);
        var gameStates = await _gameStatisticsRepository.GetGamesGroupedByState(cancellationToken);
        var mostPlayedGames = await _gameStatisticsRepository.GetMostPlayedGames(4, cancellationToken);
        var topPlayers = await _playerRepository.GetTopPlayers(4, cancellationToken);
        var recentlyAddedGames = await _gameRepository.GetRecentlyAddedGames(4, cancellationToken);
        var sessionStartTimes = await _sessionRepository.GetSessionStartTimes(cancellationToken);

        return new DashboardStatisticsDto
        {
            TotalGames = totalGames,
            ActivePlayers = activePlayers,
            SessionsPlayed = sessionsPlayed,
            TotalPlayedTime = totalPlayedTime,
            TotalCollectionValue = totalCollectionValue,
            AvgGamePrice = avgGamePrice,
            ExpansionsOwned = expansionsOwned,
            AvgSessionTime = avgSessionTime,
            RecentActivities = recentSessions.ToRecentActivityListDto(),
            Collection = gameStates.ToListDto(),
            MostPlayedGames = mostPlayedGames.ToListDto(),
            TopPlayers = topPlayers.ToListDto(),
            RecentAddedGames = recentlyAddedGames.ToRecentAddedGameListDto(),
            SessionsByDayOfWeek = PlayByDayBuckets.Count(sessionStartTimes, _dateTimeProvider)
                .Select(x => new PlayByDay { DayOfWeek = x.Key, PlayCount = x.Value })
                .ToList()
        };
    }
}
