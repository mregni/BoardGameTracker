using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Badges;

public class BadgeRepository : EfRepository<Badge>, IBadgeRepository
{
    private readonly MainDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BadgeRepository(MainDbContext context, IDateTimeProvider dateTimeProvider) : base(context)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
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

    public async Task<bool> AwardBadgeToPlayer(int playerId, int badgeId)
    {
        if (!await _context.Badges.AnyAsync(x => x.Id == badgeId))
        {
            throw new EntityNotFoundException(nameof(Badge), badgeId);
        }

        if (!await _context.Players.AnyAsync(x => x.Id == playerId))
        {
            throw new EntityNotFoundException(nameof(Player), playerId);
        }

        var alreadyAwarded = await _context.Set<PlayerBadge>()
            .AnyAsync(x => x.BadgeId == badgeId && x.PlayerId == playerId);
        if (alreadyAwarded)
        {
            return false;
        }

        _context.Set<PlayerBadge>().Add(new PlayerBadge(playerId, badgeId, _dateTimeProvider.UtcNow));
        return true;
    }
}
