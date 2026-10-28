using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Players;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerRepositoryLeaderboardTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly PlayerRepository _repository;

    public PlayerRepositoryLeaderboardTests()
    {
        _context = new MainDbContext(new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider
            .Setup(x => x.ConvertToUtc(It.IsAny<DateTime>()))
            .Returns((DateTime local) => DateTime.SpecifyKind(local.AddHours(-2), DateTimeKind.Utc));

        _repository = new PlayerRepository(
            _context,
            Mock.Of<IReadRepository<Session>>(),
            Mock.Of<IReadRepository<PlayerSession>>(),
            dateTimeProvider.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldUseLocalDayBoundaries_ForThePeriod()
    {
        var game = new Game("Catan", true, GameState.Owned);
        var player = new Player("Alice");
        _context.AddRange(game, player);
        await _context.SaveChangesAsync();

        var lateEvening = new DateTime(2026, 3, 2, 21, 30, 0, DateTimeKind.Utc);
        var afterLocalMidnight = new DateTime(2026, 3, 2, 22, 30, 0, DateTimeKind.Utc);
        var early = new Session(game.Id, lateEvening, lateEvening.AddMinutes(20), string.Empty);
        early.AddPlayerSession(player.Id, null, false, false);
        var late = new Session(game.Id, afterLocalMidnight, afterLocalMidnight.AddMinutes(40), string.Empty);
        late.AddPlayerSession(player.Id, null, false, true);
        _context.AddRange(early, late);
        await _context.SaveChangesAsync();

        var marchThird = await _repository.GetLeaderboardRows(new LeaderboardFilter(From: new DateOnly(2026, 3, 3), To: new DateOnly(2026, 3, 3)));
        var marchSecond = await _repository.GetLeaderboardRows(new LeaderboardFilter(From: new DateOnly(2026, 3, 2), To: new DateOnly(2026, 3, 2)));

        marchThird.Should().ContainSingle().Which.Should().BeEquivalentTo(new { PlayCount = 1, WinCount = 1 });
        marchSecond.Should().ContainSingle().Which.Should().BeEquivalentTo(new { PlayCount = 1, WinCount = 0 });
    }
}
