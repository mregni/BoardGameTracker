using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Players;

public class PlayerRepository : IPlayerRepository
{
    private readonly MainDbContext _dbContext;

    public PlayerRepository(MainDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Player>> GetPlayers()
    {
        return _dbContext.Players
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task CreatePlayer(Player player)
    {
        await _dbContext.Players.AddAsync(player);
        await _dbContext.SaveChangesAsync();
    }
    
    public Task<Player?> GetPlayerById(int id)
    {
        return _dbContext.Players.SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task DeletePlayer(Player player)
    {
        _dbContext.Remove(player);
        return _dbContext.SaveChangesAsync();
    }

    public Task<int> GetPlayCount(int id)
    {
        return _dbContext.Players
            .Include(x => x.Plays)
            .Where(x => x.Id == id)
            .Select(x => x.Plays.Count)
            .FirstAsync();
    }

    public Task<int> GetBestGameId(int id)
    {
        return _dbContext.Players
            .Include(x => x.Plays)
            .Where(x => x.Id == id)
            .SelectMany(x => x.Plays)
            .Where(x => x.Won)
            .GroupBy(x => x.Play.GameId)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefaultAsync();
    }

    public Task<string?> GetFavoriteColor(int id)
    {
        return _dbContext.Players
            .Include(x => x.Plays)
            .Where(x => x.Id == id)
            .SelectMany(x => x.Plays)
            .Where(x => x.Color != "" && x.Color != null)
            .GroupBy(x => x.Color)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefaultAsync();
    }

    public Task<int> GetTotalWinCount(int id)
    {
        return _dbContext.Players
            .Include(x => x.Plays)
            .Where(x => x.Id == id)
            .SelectMany(x => x.Plays)
            .CountAsync(x => x.Won);
    }

    public Task<double> GetPlayLengthInMinutes(int id)
    {
        return _dbContext.Players
            .Include(x => x.Plays)
            .ThenInclude(x => x.Play)
            .Where(x => x.Id == id)
            .SelectMany(x => x.Plays)
            .SumAsync(x => (x.Play.End - x.Play.Start).TotalMinutes);
    }
}