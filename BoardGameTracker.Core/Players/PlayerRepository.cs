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
            .Include(x => x.Badges)
            .SingleOrDefaultAsync(x => x.Id == id);
    }

    public override Task<List<Player>> GetAllAsync()
    {
        return _dbContext.Players
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
    public async Task<Game?> GetBestGame(int id)
    {
        var gameId = await _dbContext.Players
            .Include(x => x.PlayerSessions)
            .Where(x => x.Id == id)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Won)
            .GroupBy(x => x.Session.GameId)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefaultAsync();

        return await _dbContext.Games.FirstOrDefaultAsync(x => x.Id == gameId);
    }


    public Task<double> GetPlayLengthInMinutes(int id)
    {
        return _dbContext.Players
            .Include(x => x.PlayerSessions)
            .ThenInclude(x => x.Session)
            .Where(x => x.Id == id)
            .SelectMany(x => x.PlayerSessions)
            .SumAsync(x => (x.Session.End - x.Session.Start).TotalMinutes);
    }

    public Task<int> GetDistinctGameCount(int id)
    {
        return _dbContext.Sessions
            .Include(x => x.PlayerSessions)
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == id))
            .Select(x => x.GameId)
            .Distinct()
            .CountAsync();
    }

    public Task<int> CountAsync()
    {
        return _dbContext.Players.CountAsync();
    }

    public Task<int> GetTotalPlayCount(int id)
    {
        return _dbContext.Sessions
            .Include(x => x.PlayerSessions)
            .CountAsync(x => x.PlayerSessions.Any(y => y.PlayerId == id));
    }

    public Task<int> GetWinCount(int id, int gameId)
    {
        return _dbContext.Sessions
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId && x.PlayerSessions.Any(y => y.Player.Id == id && y.Won))
            .CountAsync();
    }

    public Task<int> GetTotalWinCount(int id)
    {
        return _dbContext.Players
            .Include(x => x.PlayerSessions)
            .Where(x => x.Id == id)
            .SelectMany(x => x.PlayerSessions)
            .CountAsync(x => x.Won);
    }

    public Task<List<Session>> GetSessions(int id)
    {
        return _dbContext.Sessions
            .Include(x => x.PlayerSessions)
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == id))
            .ToListAsync();
    }
}