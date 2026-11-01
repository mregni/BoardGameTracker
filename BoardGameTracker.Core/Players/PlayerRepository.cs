using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Leaderboard;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Players.Specifications;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Players;

public class PlayerRepository : EfRepository<Player>, IPlayerRepository
{
    private readonly MainDbContext _dbContext;
    private readonly IReadRepository<Session> _sessionReadRepository;
    private readonly IReadRepository<PlayerSession> _playerSessionReadRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlayerRepository(
        MainDbContext dbContext,
        IReadRepository<Session> sessionReadRepository,
        IReadRepository<PlayerSession> playerSessionReadRepository,
        IDateTimeProvider dateTimeProvider) : base(dbContext)
    {
        _dbContext = dbContext;
        _sessionReadRepository = sessionReadRepository;
        _playerSessionReadRepository = playerSessionReadRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public override Task<Player?> GetByIdAsync(int id)
    {
        return SingleOrDefaultAsync(new PlayerByIdWithBadgesSpec(id));
    }

    public override Task<List<Player>> GetAllAsync()
    {
        return ListAsync(new PlayersOrderedByNameSpec());
    }

    public async Task<List<MostPlayedGame>> GetMostPlayedGames(int playerId, int count)
    {
        return await _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId)
            .GroupBy(x => new { x.Session.GameId, x.Session.Game.Title, x.Session.Game.Image })
            .OrderByDescending(x => x.Count())
            .Take(count)
            .Select(x => new MostPlayedGame
            {
                Id = x.Key.GameId,
                Title = x.Key.Title,
                Image = x.Key.Image ?? string.Empty,
                TotalSessions = x.Count(),
                TotalWins = x.Count(ps => ps.Won),
                WinningPercentage = (double)x.Count(ps => ps.Won) / x.Count() * 100
            })
            .ToListAsync();
    }

    public Task<double> GetPlayLengthInMinutes(int id)
    {
        return _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(ps => ps.PlayerId == id)
            .SumAsync(ps => (ps.Session.End - ps.Session.Start).TotalMinutes);
    }

    public Task<int> GetDistinctGameCount(int id)
    {
        return _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == id))
            .Select(x => x.GameId)
            .Distinct()
            .CountAsync();
    }

    public Task<int> GetTotalPlayCount(int id)
    {
        return _sessionReadRepository.CountAsync(new SessionsByPlayerSpec(id));
    }

    public Task<int> GetTotalWinCount(int id)
    {
        return _playerSessionReadRepository.CountAsync(new WonPlayerSessionsByPlayerSpec(id));
    }

    public async Task<(int Won, int Lost)> GetCoopResults(int id)
    {
        var results = await _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == id && x.Session.IsCoop)
            .GroupBy(x => x.Won)
            .Select(x => new { Won = x.Key, Count = x.Count() })
            .ToListAsync();

        return (results.Where(x => x.Won).Sum(x => x.Count), results.Where(x => !x.Won).Sum(x => x.Count));
    }

    public async Task<List<PlayerRival>> GetRivals(int id)
    {
        return await (
                from me in _dbContext.PlayerSessions
                join opponent in _dbContext.PlayerSessions on me.SessionId equals opponent.SessionId
                where me.PlayerId == id && opponent.PlayerId != id && !me.Session.IsCoop
                group new { me.Won, OpponentWon = opponent.Won } by opponent.PlayerId
                into rival
                select new PlayerRival
                {
                    PlayerId = rival.Key,
                    Sessions = rival.Count(),
                    Wins = rival.Count(x => x.Won && !x.OpponentWon),
                    Losses = rival.Count(x => x.OpponentWon && !x.Won)
                })
            .OrderByDescending(x => x.Sessions)
            .ThenBy(x => x.PlayerId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>> GetTopPlayers(int count, CancellationToken cancellationToken = default)
    {
        var result = await PlayerTotals()
            .OrderByDescending(x => x.PlayCount)
            .ThenBy(x => x.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.Id, x.Name, x.Image, x.PlayCount, x.WinCount)).ToList();
    }

    public async Task<List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>> GetWinTotals(CancellationToken cancellationToken = default)
    {
        var result = await PlayerTotals()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.Id, x.Name, x.Image, x.PlayCount, x.WinCount)).ToList();
    }

    public Task<int> CountActivePlayersSince(DateTime since, CancellationToken cancellationToken = default)
    {
        return _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.Session.Start >= since)
            .Select(x => x.PlayerId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<(int PlayCount, int WinCount)> GetPlayCountsSince(int playerId, DateTime since, CancellationToken cancellationToken = default)
    {
        var counts = await _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId && x.Session.Start >= since)
            .GroupBy(x => x.PlayerId)
            .Select(g => new { PlayCount = g.Count(), WinCount = g.Count(x => x.Won) })
            .FirstOrDefaultAsync(cancellationToken);

        return counts == null ? (0, 0) : (counts.PlayCount, counts.WinCount);
    }

    private IQueryable<PlayerTotal> PlayerTotals()
    {
        return _dbContext.PlayerSessions
            .AsNoTracking()
            .GroupBy(x => x.PlayerId)
            .Select(g => new PlayerTotal
            {
                Id = g.Key,
                Name = g.First().Player.Name,
                Image = g.First().Player.Image,
                PlayCount = g.Count(),
                WinCount = g.Count(x => x.Won)
            });
    }

    private sealed class PlayerTotal
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Image { get; init; }
        public int PlayCount { get; init; }
        public int WinCount { get; init; }
    }

    public async Task<List<LeaderboardRow>> GetLeaderboardRows(LeaderboardFilter filter, CancellationToken cancellationToken = default)
    {
        var plays = FilteredPlays(filter);

        var totals = await plays
            .GroupBy(x => new { x.PlayerId, x.Player.Name, x.Player.Image })
            .Select(g => new
            {
                g.Key.PlayerId,
                g.Key.Name,
                g.Key.Image,
                PlayCount = g.Count(),
                WinCount = g.Count(x => x.Won),
                MinutesPlayed = g.Sum(x => (x.Session.End - x.Session.Start).TotalMinutes),
                CoopWinCount = g.Count(x => x.Won && x.Session.IsCoop),
            })
            .ToListAsync(cancellationToken);

        var scored = await plays
            .Where(x => x.Score != null && !x.Session.IsCoop && x.Session.Game.HasScoring)
            .Where(x => x.Session.PlayerSessions.Count(other => other.Score != null) >= LeaderboardService.MinimumPlayersForPodium)
            .Select(x => new ScoredPlay(x.SessionId, x.PlayerId, x.Won, x.Score!.Value))
            .ToListAsync(cancellationToken);

        var podiums = scored
            .GroupBy(x => x.SessionId)
            .Select(session => session.ToList())
            .SelectMany(PodiumPlayers)
            .GroupBy(playerId => playerId)
            .ToDictionary(g => g.Key, g => g.Count());

        return totals
            .Select(x => new LeaderboardRow(x.PlayerId, x.Name, x.Image, x.PlayCount, x.WinCount, podiums.GetValueOrDefault(x.PlayerId), x.MinutesPlayed, x.CoopWinCount))
            .ToList();
    }

    private IQueryable<PlayerSession> FilteredPlays(LeaderboardFilter filter)
    {
        var plays = _dbContext.PlayerSessions.AsNoTracking();

        if (filter.From.HasValue)
        {
            var from = _dateTimeProvider.ConvertToUtc(filter.From.Value.ToDateTime(TimeOnly.MinValue));
            plays = plays.Where(x => x.Session.Start >= from);
        }

        if (filter.To.HasValue)
        {
            var until = _dateTimeProvider.ConvertToUtc(filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            plays = plays.Where(x => x.Session.Start < until);
        }

        if (filter.GameId.HasValue)
        {
            var gameId = filter.GameId.Value;
            plays = plays.Where(x => x.Session.GameId == gameId);
        }

        return plays;
    }

    private static IEnumerable<int> PodiumPlayers(List<ScoredPlay> session)
    {
        var winners = session.Where(x => x.Won).ToList();
        var losers = session.Where(x => !x.Won).ToList();
        var lowestWins = winners.Count > 0 && losers.Count > 0 && winners.Max(x => x.Score) < losers.Min(x => x.Score);

        return session
            .Where(play => 1 + session.Count(other => IsBetter(other, play, lowestWins)) <= 3)
            .Select(play => play.PlayerId);
    }

    private static bool IsBetter(ScoredPlay candidate, ScoredPlay play, bool lowestWins)
    {
        if (candidate.Won != play.Won)
        {
            return candidate.Won;
        }

        return lowestWins ? candidate.Score < play.Score : candidate.Score > play.Score;
    }

    private sealed record ScoredPlay(int SessionId, int PlayerId, bool Won, double Score);
}