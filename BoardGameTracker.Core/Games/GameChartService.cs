using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class GameChartService : IGameChartService
{
    private readonly IGameRepository _gameRepository;
    private readonly IReadRepository<Session> _sessionRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<GameChartService> _logger;

    public GameChartService(
        IGameRepository gameRepository,
        IReadRepository<Session> sessionRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<GameChartService> logger)
    {
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<PlayByDay>> GetPlayByDayChart(int id)
    {
        _logger.LogDebug("Getting play-by-day chart for game {GameId}", id);
        var list = await _gameStatisticsRepository.GetPlayByDayChart(id);
        return Enum.GetValues(typeof(DayOfWeek))
            .Cast<DayOfWeek>()
            .OrderBy(day => ((int)day + 6) % 7)
            .ToDictionary(day => day, day => list.SingleOrDefault(y => y.Key == day)?.Count() ?? 0)
            .Select(x => new PlayByDay {DayOfWeek = x.Key, PlayCount = x.Value});
    }

    public async Task<IEnumerable<PlayerCount>> GetPlayerCountChart(int id)
    {
        _logger.LogDebug("Getting player count chart for game {GameId}", id);
        var list = await _gameStatisticsRepository.GetPlayerCountChart(id);
        return list.Select(x => new PlayerCount {PlayCount = x.Count(), Players = x.Key});
    }

    public async Task<List<TopPlayerDto>> GetTopPlayers(int id)
    {
        _logger.LogDebug("Getting top players for game {GameId}", id);
        var sessions = await _sessionRepository.ListAsync(new SessionsByGameSpec(id));
        var playerSessions = sessions
            .SelectMany(x => x.PlayerSessions)
            .GroupBy(x => x.PlayerId)
            .ToList();

        return playerSessions
            .Select(TopPlayerDto.CreateTopPlayer)
            .Where(x => x.Wins > 0)
            .OrderByDescending(x => x.Wins)
            .Take(Constants.Game.TopPlayersCount)
            .ToList();
    }

    public async Task<Dictionary<DateTime, XValue[]>?> GetPlayerScoringChart(int id)
    {
        _logger.LogDebug("Getting player scoring chart for game {GameId}", id);
        var hasScoring = await _gameRepository.FirstOrDefaultAsync(new GameHasScoringSpec(id));
        if (hasScoring is not true)
        {
            return null;
        }

        var cutoff = _dateTimeProvider.UtcNow.AddDays(-Constants.Game.ChartHistoryDays);
        var sessions = await _sessionRepository.ListAsync(new SessionsByGameSinceSpec(id, cutoff));

        var uniquePlayerIds = sessions
            .SelectMany(session => session.PlayerSessions)
            .Select(ps => ps.PlayerId)
            .Distinct()
            .ToList();

        var chartData = new Dictionary<DateTime, XValue[]>();

        foreach (var session in sessions)
        {
            var playerIdsInSession = session.PlayerSessions.Select(ps => ps.PlayerId).ToHashSet();

            var participatingPlayers = session.PlayerSessions
                .Select(ps => new XValue
                {
                    Id = ps.PlayerId,
                    Value = ps.Score
                });

            var nonParticipatingPlayers = uniquePlayerIds
                .Where(playerId => !playerIdsInSession.Contains(playerId))
                .Select(playerId => new XValue
                {
                    Id = playerId,
                    Value = null
                });

            var allPlayerValues = participatingPlayers.Concat(nonParticipatingPlayers).ToArray();
            chartData.TryAdd(session.Start, allPlayerValues);
        }

        return chartData;
    }

    public async Task<List<ScoreRank>> GetScoringRankedChart(int id, double? averageScore)
    {
        _logger.LogDebug("Getting scoring ranked chart for game {GameId}", id);
        var list = new List<ScoreRank>();
        var highestScoring = await _gameStatisticsRepository.GetHighestScoringPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeHighestScoreRank(highestScoring));

        var highestLosing = await _gameStatisticsRepository.GetHighestLosingPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeHighestLosingRank(highestLosing));

        list.AddIfNotNull(ScoreRank.MakeAverageRank(averageScore));

        var lowestWinning = await _gameStatisticsRepository.GetLowestWinning(id);
        list.AddIfNotNull(ScoreRank.MakeLowestWinningRank(lowestWinning));

        var lowest = await _gameStatisticsRepository.GetLowestScoringPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeLowestScoreRank(lowest));

        return list;
    }
}
