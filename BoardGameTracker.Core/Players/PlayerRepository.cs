using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Players;

public class PlayerRepository : IPlayerRepository
{
    private readonly MainContext _context;

    public PlayerRepository(MainContext context)
    {
        _context = context;
    }

    public Task<List<Player>> GetPlayers()
    {
        return _context.Players
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task CreatePlayer(Player player)
    {
        await _context.Players.AddAsync(player);
        await _context.SaveChangesAsync();
    }
    
    public Task<Player?> GetPlayerById(int id)
    {
        return _context.Players.SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task DeletePlayer(Player player)
    {
        _context.Remove(player);
        return _context.SaveChangesAsync();
    }
}