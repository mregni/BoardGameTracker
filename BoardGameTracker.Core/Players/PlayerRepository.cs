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
}