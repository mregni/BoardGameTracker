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

    public async Task<Dictionary<int, List<Badge>>> GetPlayerBadgesBatchAsync(IEnumerable<int> playerIds)
    {
        var playerIdsList = playerIds.ToList();

        var badges = await _context.Badges
            .Where(b => b.Players.Any(p => playerIdsList.Contains(p.Id)))
            .Select(b => new
            {
                Badge = b,
                PlayerIds = b.Players.Where(p => playerIdsList.Contains(p.Id)).Select(p => p.Id)
            })
            .ToListAsync();

        var result = playerIdsList.ToDictionary(id => id, _ => new List<Badge>());

        foreach (var item in badges)
        {
            foreach (var playerId in item.PlayerIds)
            {
                result[playerId].Add(item.Badge);
            }
        }

        return result;
    }

    public async Task<bool> AwardBatchToPlayer(int playerId, int badgeId)
    {
        var badge = await _context.Badges
            .Include(x => x.Players)
            .SingleOrDefaultAsync(x => x.Id == badgeId);

        if (badge == null)
            throw new BoardGameTracker.Common.Exceptions.EntityNotFoundException(nameof(Badge), badgeId);

        var player = await _context.Players.SingleOrDefaultAsync(x => x.Id == playerId);
        if (player == null)
            throw new BoardGameTracker.Common.Exceptions.EntityNotFoundException(nameof(Player), playerId);

        if (badge.Players.Any(p => p.Id == playerId))
            return false;

        badge.Players.Add(player);
        return true;
    }
}
