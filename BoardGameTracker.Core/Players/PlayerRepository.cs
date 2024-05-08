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

    public Task<List<Player>> GetList()
    {
        return _dbContext.Players
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<Player> Create(Player player)
    {
        await _dbContext.Players.AddAsync(player);
        await _dbContext.SaveChangesAsync();
        return player;
    }
    
    public Task<Player?> GetById(int id)
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

    public Task<List<Play>> GetPlaysForPlayer(int id, int skip, int? take)
    {
        var query = _dbContext.Plays
            .Include(x => x.Location)
            .Include(x => x.Players)
            .Where(x => x.Players.Any(y => y.PlayerId == id))
            .OrderByDescending(x => x.Start)
            .Skip(skip);

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }
        
        return query.ToListAsync();
    }

    public async Task<Player> Update(Player player)
    {
        var dbPlayer = await _dbContext.Players
            .SingleOrDefaultAsync(x => x.Id == player.Id);
        if (dbPlayer != null)
        {
            dbPlayer.Name = player.Name;
            dbPlayer.Image = player.Image;
            await _dbContext.SaveChangesAsync();
        }

        return player;
    }

    public Task<int> GetDistinctGameCount(int id)
    {
        return _dbContext.Plays
            .Include(x => x.Players)
            .Where(x => x.Players.Any(y => y.PlayerId == id))
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
        return _dbContext.Plays
            .Include(x => x.Players)
            .CountAsync(x => x.Players.Any(y => y.PlayerId == id));
    }
}