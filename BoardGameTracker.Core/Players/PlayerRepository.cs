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
    public async Task<Game?> GetBestGame(int id)
    {
        return await _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(ps => ps.PlayerId == id && ps.Won)
            .GroupBy(ps => ps.Session.Game)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MostPlayedGame>> GetMostPlayedGames(int playerId, int count)
    {
        return await _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId)
            .GroupBy(x => x.Session.Game)
            .OrderByDescending(x => x.Count())
            .Take(count)
            .Select(x => new MostPlayedGame
            {
                Id = x.Key.Id,
                Title = x.Key.Title,
                Image = x.Key.Image ?? string.Empty,
                TotalSessions = x.Count(),
                TotalWins = x.Count(ps => ps.Won),
                WinningPercentage = x.Count() > 0
                    ? (double)x.Count(ps => ps.Won) / x.Count() * 100
                    : 0
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

    public Task<int> CountAsync()
    {
        return base.CountAsync();
    }

    public Task<int> GetTotalPlayCount(int id)
    {
        return _sessionReadRepository.CountAsync(new SessionsByPlayerSpec(id));
    }

    public Task<int> GetWinCount(int id, int gameId)
    {
        return _sessionReadRepository.CountAsync(new WonSessionsByPlayerAndGameSpec(id, gameId));
    }

    public Task<int> GetTotalWinCount(int id)
    {
        return _playerSessionReadRepository.CountAsync(new WonPlayerSessionsByPlayerSpec(id));
    }

    public async Task<List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>> GetTopPlayers(int count)
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
            .ToListAsync();

        return result.Select(x => (x.Id, x.Name, x.Image, x.PlayCount, x.WinCount)).ToList();
    }
}