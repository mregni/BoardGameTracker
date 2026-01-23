using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ISessionRepository _sessionRepository;

    public DashboardService(
        IGameRepository gameRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        IPlayerRepository playerRepository,
        ISessionRepository sessionRepository)
    {
        _gameRepository = gameRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _playerRepository = playerRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<DashboardStatisticsDto> GetStatistics()
    {
        var totalGames = await _gameRepository.CountAsync();
        var activePlayers = await _playerRepository.CountAsync();
        var sessionsPlayed = await _sessionRepository.CountAsync();
        var totalPlayedTime = await _sessionRepository.GetTotalPlayTime();
        var totalCollectionValue = await _gameStatisticsRepository.GetTotalPayedAsync();
        var avgGamePrice = await _gameStatisticsRepository.GetMeanPayedAsync();
        var expansionsOwned = await _gameRepository.GetTotalExpansionCount();
        var avgSessionTime = await _sessionRepository.GetMeanPlayTime();

        var recentSessions = await _sessionRepository.GetRecentSessions(4);
        var gameStates = await _gameStatisticsRepository.GetGamesGroupedByState();
        var mostPlayedGames = await _gameStatisticsRepository.GetMostPlayedGames(4);
        var topPlayers = await _playerRepository.GetTopPlayers(4);
        var recentlyAddedGames = await _gameRepository.GetRecentlyAddedGames(4);
        var sessionsByDayOfWeek = await _sessionRepository.GetSessionsByDayOfWeek();

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
            RecentActivities = recentSessions.ToDtoList(),
            Collection = gameStates.ToDtoList(),
            MostPlayedGames = mostPlayedGames.ToDtoList(),
            TopPlayers = topPlayers.ToDtoList(),
            RecentAddedGames = recentlyAddedGames.ToDtoList(),
            SessionsByDayOfWeek = sessionsByDayOfWeek.ToDtoList()
        };
    }
}
