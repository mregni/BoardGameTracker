using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Players;

public class PlayerRepository : CrudHelper<Player>, IPlayerRepository
{
    private readonly MainDbContext _dbContext;

    public PlayerRepository(MainDbContext dbContext): base(dbContext)
    {
        _dbContext = dbContext;
    }

    public override Task<Player?> GetByIdAsync(int id)
    {
        return _dbContext.Players
            .AsNoTracking()
            .Include(x => x.Badges)
            .SingleOrDefaultAsync(x => x.Id == id);
    }

    public override Task<List<Player>> GetAllAsync()
    {
        return _dbContext.Players
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
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

    public async Task<List<Game>> GetMostPlayedGames(int playerId, int count)
    {
        return await _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId)
            .GroupBy(x => x.Session.Game)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .Take(count)
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
        return _dbContext.Players
            .AsNoTracking()
            .CountAsync();
    }

    public Task<int> GetTotalPlayCount(int id)
    {
        return _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(x => x.PlayerSessions.Any(y => y.PlayerId == id));
    }

    public Task<int> GetPlayCount(int playerId, int gameId)
    {
        return _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(x => x.GameId == gameId && x.PlayerSessions.Any(y => y.PlayerId == playerId));
    }

    public Task<int> GetWinCount(int id, int gameId)
    {
        return _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId && x.PlayerSessions.Any(y => y.Player.Id == id && y.Won))
            .CountAsync();
    }

    public Task<int> GetTotalWinCount(int id)
    {
        return _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(ps => ps.PlayerId == id && ps.Won)
            .CountAsync();
    }
}