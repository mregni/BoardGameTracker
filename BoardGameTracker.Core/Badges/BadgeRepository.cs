using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Badges;

public class BadgeRepository : CrudHelper<Badge>,IBadgeRepository
{
    private readonly MainDbContext _context;
    public BadgeRepository(MainDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<List<Badge>> GetPlayerBadgesAsync(int playerId)
    {
        return _context.Badges
            .Where(x => x.Players.Any(p => p.Id == playerId))
            .ToListAsync();
    }

    public async Task AwardBatchToPlayer(int playerId, int badgeId)
    {
        var badge = await _context.Badges
            .Include(x => x.Players)
            .SingleOrDefaultAsync(x => x.Id == badgeId);
        var player = await _context.Players.SingleOrDefaultAsync(x => x.Id == playerId);
        
        if (badge == null || player == null)
        {
            return;
        }
        
        badge.Players.Add(player);
        await _context.SaveChangesAsync();
    }
}
