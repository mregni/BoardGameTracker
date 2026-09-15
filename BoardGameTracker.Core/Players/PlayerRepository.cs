using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
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

    public PlayerRepository(
        MainDbContext dbContext,
        IReadRepository<Session> sessionReadRepository,
        IReadRepository<PlayerSession> playerSessionReadRepository) : base(dbContext)
    {
        _dbContext = dbContext;
        _sessionReadRepository = sessionReadRepository;
        _playerSessionReadRepository = playerSessionReadRepository;
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

    public async Task<List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>> GetTopPlayers(int count, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.PlayerSessions
            .AsNoTracking()
            .Include(x => x.Player)
            .GroupBy(x => x.PlayerId)
            .Select(g => new
            {
                Id = g.Key,
                Name = g.First().Player.Name,
                Image = g.First().Player.Image,
                PlayCount = g.Count(),
                WinCount = g.Count(x => x.Won)
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(count)
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.Id, x.Name, x.Image, x.PlayCount, x.WinCount)).ToList();
    }

    public async Task<List<LeaderboardRow>> GetLeaderboardRows(CancellationToken cancellationToken = default)
    {
        var totals = await _dbContext.PlayerSessions
            .AsNoTracking()
            .GroupBy(x => x.PlayerId)
            .Select(g => new
            {
                Id = g.Key,
                Name = g.First().Player.Name,
                Image = g.First().Player.Image,
                PlayCount = g.Count(),
                WinCount = g.Count(x => x.Won),
                MinutesPlayed = g.Sum(x => (x.Session.End - x.Session.Start).TotalMinutes),
            })
            .ToListAsync(cancellationToken);

        var scored = await _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.Score != null && x.Session.Game.HasScoring)
            .Select(x => new { x.SessionId, x.PlayerId, Score = x.Score!.Value })
            .ToListAsync(cancellationToken);

        var podiums = scored
            .GroupBy(x => x.SessionId)
            .SelectMany(session => session
                .OrderByDescending(x => x.Score)
                .Select((x, index) => new { x.PlayerId, Place = index + 1 })
                .Where(x => x.Place <= 3))
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.Count());

        return totals
            .Select(x => new LeaderboardRow(x.Id, x.Name, x.Image, x.PlayCount, x.WinCount, podiums.GetValueOrDefault(x.Id), x.MinutesPlayed))
            .ToList();
    }
}