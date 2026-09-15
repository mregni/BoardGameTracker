using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Badges;

public class BadgeRepositoryTests
{
    private static MainDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new MainDbContext(options);
    }

    private static async Task<(int BadgeId, int PlayerId)> SeedAsync(string databaseName)
    {
        await using var context = CreateContext(databaseName);
        var badge = Badge.CreateWithId(1, "title", "description", BadgeType.Sessions, "badge.png", BadgeLevel.Green);
        var player = new Player("Alice");
        context.Badges.Add(badge);
        context.Players.Add(player);
        await context.SaveChangesAsync();
        return (badge.Id, player.Id);
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldLinkPlayerWithoutLoadingHolders()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using (var context = CreateContext(databaseName))
        {
            var repository = new BadgeRepository(context);

            var awarded = await repository.AwardBadgeToPlayer(playerId, badgeId);
            await context.SaveChangesAsync();

            awarded.Should().BeTrue();
            context.ChangeTracker.Entries<Badge>().Should().BeEmpty();
        }

        await using var verify = CreateContext(databaseName);
        var badge = await verify.Badges.Include(x => x.Players).SingleAsync();
        badge.Players.Select(x => x.Id).Should().Equal(playerId);
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldReturnFalse_WhenAlreadyAwarded()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using var context = CreateContext(databaseName);
        var repository = new BadgeRepository(context);
        await repository.AwardBadgeToPlayer(playerId, badgeId);
        await context.SaveChangesAsync();

        var awardedAgain = await repository.AwardBadgeToPlayer(playerId, badgeId);

        awardedAgain.Should().BeFalse();
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldThrow_WhenBadgeOrPlayerIsMissing()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using var context = CreateContext(databaseName);
        var repository = new BadgeRepository(context);

        var missingBadge = async () => await repository.AwardBadgeToPlayer(playerId, badgeId + 1);
        var missingPlayer = async () => await repository.AwardBadgeToPlayer(playerId + 1, badgeId);

        await missingBadge.Should().ThrowAsync<EntityNotFoundException>();
        await missingPlayer.Should().ThrowAsync<EntityNotFoundException>();
    }
}
