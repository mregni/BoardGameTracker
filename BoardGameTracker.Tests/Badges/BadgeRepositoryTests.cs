using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Badges;

public class BadgeRepositoryTests
{
    private static readonly DateTime AwardedAt = new(2026, 10, 21, 18, 30, 0, DateTimeKind.Utc);
    private readonly Mock<IDateTimeProvider> _clock = new();

    public BadgeRepositoryTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(AwardedAt);
    }

    private void VerifyNoOtherCalls()
    {
        _clock.VerifyNoOtherCalls();
    }

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
            var repository = new BadgeRepository(context, _clock.Object);

            var awarded = await repository.AwardBadgeToPlayer(playerId, badgeId);
            await context.SaveChangesAsync();

            awarded.Should().BeTrue();
            context.ChangeTracker.Entries<Badge>().Should().BeEmpty();
        }

        await using var verify = CreateContext(databaseName);
        var badge = await verify.Badges.Include(x => x.Players).SingleAsync();
        badge.Players.Select(x => x.Id).Should().Equal(playerId);
        _clock.VerifyGet(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldReturnFalse_WhenAlreadyAwarded()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using var context = CreateContext(databaseName);
        var repository = new BadgeRepository(context, _clock.Object);
        await repository.AwardBadgeToPlayer(playerId, badgeId);
        await context.SaveChangesAsync();

        var awardedAgain = await repository.AwardBadgeToPlayer(playerId, badgeId);

        awardedAgain.Should().BeFalse();
        _clock.VerifyGet(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldThrow_WhenBadgeOrPlayerIsMissing()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using var context = CreateContext(databaseName);
        var repository = new BadgeRepository(context, _clock.Object);

        var missingBadge = async () => await repository.AwardBadgeToPlayer(playerId, badgeId + 1);
        var missingPlayer = async () => await repository.AwardBadgeToPlayer(playerId + 1, badgeId);

        await missingBadge.Should().ThrowAsync<EntityNotFoundException>();
        await missingPlayer.Should().ThrowAsync<EntityNotFoundException>();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldStampAwardedAtWithTheCurrentUtcTime()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using (var context = CreateContext(databaseName))
        {
            var repository = new BadgeRepository(context, _clock.Object);
            await repository.AwardBadgeToPlayer(playerId, badgeId);
            await context.SaveChangesAsync();
        }

        await using var verify = CreateContext(databaseName);
        var playerBadge = await verify.Set<PlayerBadge>().SingleAsync();
        playerBadge.PlayerId.Should().Be(playerId);
        playerBadge.BadgeId.Should().Be(badgeId);
        playerBadge.AwardedAt.Should().Be(AwardedAt);
        _clock.VerifyGet(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldKeepTheOriginalAwardedAt_WhenEvaluatedAgain()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using (var context = CreateContext(databaseName))
        {
            await new BadgeRepository(context, _clock.Object).AwardBadgeToPlayer(playerId, badgeId);
            await context.SaveChangesAsync();
        }

        _clock.Setup(x => x.UtcNow).Returns(AwardedAt.AddDays(3));

        await using (var context = CreateContext(databaseName))
        {
            var awardedAgain = await new BadgeRepository(context, _clock.Object).AwardBadgeToPlayer(playerId, badgeId);
            await context.SaveChangesAsync();
            awardedAgain.Should().BeFalse();
        }

        await using var verify = CreateContext(databaseName);
        var playerBadge = await verify.Set<PlayerBadge>().SingleAsync();
        playerBadge.AwardedAt.Should().Be(AwardedAt);
        _clock.VerifyGet(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldAward_WhenOnlyAnotherPlayerOrBadgeWasAwardedBefore()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (badgeId, playerId) = await SeedAsync(databaseName);

        await using (var seed = CreateContext(databaseName))
        {
            var otherPlayer = new Player("Bob");
            var otherBadge = Badge.CreateWithId(2, "other", "description", BadgeType.Sessions, "other.png", BadgeLevel.Blue);
            seed.Players.Add(otherPlayer);
            seed.Badges.Add(otherBadge);
            await seed.SaveChangesAsync();
            seed.Set<PlayerBadge>().AddRange(new PlayerBadge(otherPlayer.Id, badgeId, AwardedAt), new PlayerBadge(playerId, otherBadge.Id, AwardedAt));
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(databaseName);
        var awarded = await new BadgeRepository(context, _clock.Object).AwardBadgeToPlayer(playerId, badgeId);

        awarded.Should().BeTrue();
        _clock.VerifyGet(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerBadgesBatchAsync_ShouldGroupBadgesPerRequestedPlayer_AndKeepPlayersWithoutBadges()
    {
        var databaseName = Guid.NewGuid().ToString();
        int alice, bob, cara, dave;
        await using (var seed = CreateContext(databaseName))
        {
            var players = new[] { new Player("Alice"), new Player("Bob"), new Player("Cara"), new Player("Dave") };
            seed.Players.AddRange(players);
            seed.Badges.AddRange(
                Badge.CreateWithId(1, "shared", "description", BadgeType.Sessions, "shared.png", BadgeLevel.Green),
                Badge.CreateWithId(2, "alice", "description", BadgeType.Sessions, "alice.png", BadgeLevel.Blue),
                Badge.CreateWithId(3, "unrequested", "description", BadgeType.Sessions, "unrequested.png", BadgeLevel.Red));
            await seed.SaveChangesAsync();
            (alice, bob, cara, dave) = (players[0].Id, players[1].Id, players[2].Id, players[3].Id);
            seed.Set<PlayerBadge>().AddRange(
                new PlayerBadge(alice, 1, AwardedAt),
                new PlayerBadge(alice, 2, AwardedAt),
                new PlayerBadge(bob, 1, AwardedAt),
                new PlayerBadge(dave, 1, AwardedAt),
                new PlayerBadge(dave, 3, AwardedAt));
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(databaseName);
        var result = await new BadgeRepository(context, _clock.Object).GetPlayerBadgesBatchAsync([alice, bob, cara]);

        result.Keys.Should().BeEquivalentTo(new[] { alice, bob, cara });
        result[alice].Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 2 });
        result[bob].Select(x => x.Id).Should().Equal(1);
        result[cara].Should().BeEmpty();
        VerifyNoOtherCalls();
    }
}
