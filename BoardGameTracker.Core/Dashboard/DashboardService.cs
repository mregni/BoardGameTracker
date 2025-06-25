using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Common.Models.Dashboard;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ILocationRepository _locationRepository;

    public DashboardService(IGameRepository gameRepository, IPlayerRepository playerRepository, ISessionRepository sessionRepository, ILocationRepository locationRepository)
    {
        _gameRepository = gameRepository;
        _playerRepository = playerRepository;
        _sessionRepository = sessionRepository;
        _locationRepository = locationRepository;
    }

    public async Task<DashboardStatistics> GetStatistics()
    {
        var gameCount = await _gameRepository.CountAsync();
        var meanPayed = await _gameRepository.GetMeanPayedAsync();
        var totalPayed = await _gameRepository.GetTotalPayedAsync();
        
        var playerCount = await _playerRepository.CountAsync();
        
        var sessionCount = await _sessionRepository.CountAsync();
        var totalPlayTime = await _sessionRepository.GetTotalPlayTime();
        var meanPlayTime = await _sessionRepository.GetMeanPlayTime();
        var locationCount = await _locationRepository.CountAsync();
        var expansionCount = await _gameRepository.GetTotalExpansionCount();
        
        var result = new DashboardStatistics
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
        
        var mostWinPlayer = await _gameRepository.GetMostWins();
        if (mostWinPlayer != null)
        {
            var wins = await _playerRepository.GetWinCount(mostWinPlayer.Id);
            result.MostWinningPlayer = new MostWinningPlayer
            {
                Id = mostWinPlayer.Id,
                Image = mostWinPlayer.Image,
                Name = mostWinPlayer.Name,
                TotalWins = wins
            };
        }

        return result;
    }

    public async Task<DashboardCharts> GetCharts()
    {
        var gameStates = await _gameRepository.GetGamesGroupedByState();
        var charts = new DashboardCharts
        {
            GameState = gameStates.Select(x => new GameStateChart { Type = x.Key, GameCount = x.Count()})
        };

        return charts;
    }
}