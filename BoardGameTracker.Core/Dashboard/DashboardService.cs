using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ILocationRepository _locationRepository;

    public DashboardService(
        IGameRepository gameRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        IPlayerRepository playerRepository,
        ISessionRepository sessionRepository,
        ILocationRepository locationRepository)
    {
        _gameRepository = gameRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _playerRepository = playerRepository;
        _sessionRepository = sessionRepository;
        _locationRepository = locationRepository;
    }

    public async Task<DashboardStatisticsDto> GetStatistics()
    {
        var gameCount = await _gameRepository.CountAsync();
        var meanPayed = await _gameStatisticsRepository.GetMeanPayedAsync();
        var totalPayed = await _gameStatisticsRepository.GetTotalPayedAsync();

        var playerCount = await _playerRepository.CountAsync();
        var sessionCount = await _sessionRepository.CountAsync();
        var totalPlayTime = await _sessionRepository.GetTotalPlayTime();
        var meanPlayTime = await _sessionRepository.GetMeanPlayTime();
        var locationCount = await _locationRepository.CountAsync();
        var expansionCount = await _gameRepository.GetTotalExpansionCount();

        var result = new DashboardStatisticsDto
        {
            GameCount = gameCount,
            PlayerCount = playerCount,
            SessionCount = sessionCount,
            LocationCount = locationCount,
            TotalPlayTime = totalPlayTime,
            MeanPayed = meanPayed,
            TotalCost = totalPayed,
            MeanPlayTime = meanPlayTime,
            ExpansionCount = expansionCount
        };

        var mostWinPlayer = await _gameStatisticsRepository.GetMostWins();
        if (mostWinPlayer != null)
        {
            var wins = await _playerRepository.GetTotalWinCount(mostWinPlayer.Id);
            result.MostWinningPlayer = new MostWinningPlayerDto
            {
                Id = mostWinPlayer.Id,
                Image = mostWinPlayer.Image ?? string.Empty,
                Name = mostWinPlayer.Name,
                TotalWins = wins
            };
        }

        return result;
    }

    public async Task<DashboardChartsDto> GetCharts()
    {
        var gameStates = await _gameStatisticsRepository.GetGamesGroupedByState();
        var charts = new DashboardChartsDto
        {
            GameState = gameStates.Select(x => new GameStateChart { Type = x.Key, GameCount = x.Count()})
        };

        return charts;
    }
}