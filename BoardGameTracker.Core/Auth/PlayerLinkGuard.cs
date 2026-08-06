using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Auth;

public static class PlayerLinkGuard
{
    public static async Task EnsureLinkableAsync(MainDbContext context, int playerId, string? excludeUserId)
    {
        var playerExists = await context.Players.AnyAsync(p => p.Id == playerId);
        if (!playerExists)
        {
            throw new EntityNotFoundException(nameof(Player), playerId);
        }

        var linkedToOther = await context.Users.AnyAsync(u => u.PlayerId == playerId && u.Id != excludeUserId);
        if (linkedToOther)
        {
            throw new DomainException(Constants.Errors.PlayerAlreadyLinked);
        }
    }
}
