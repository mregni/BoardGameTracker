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
    private readonly Mock<IReadRepository<Session>> _sessionRepositoryMock = new();
    private readonly Mock<IReadRepository<PlayerSession>> _playerSessionRepositoryMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly MainDbContext _context;
    private readonly PlayerRepository _repository;

    public PlayerRepositoryLeaderboardTests()
    {
        _context = new MainDbContext(new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _dateTimeProviderMock
            .Setup(x => x.ConvertToUtc(It.Is<DateTime>(local => local.TimeOfDay == TimeSpan.Zero)))
            .Returns((DateTime local) => DateTime.SpecifyKind(local.AddHours(-2), DateTimeKind.Utc));

        _repository = new PlayerRepository(
            _context,
            _sessionRepositoryMock.Object,
            _playerSessionRepositoryMock.Object,
            _dateTimeProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _playerSessionRepositoryMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
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

        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(new DateTime(2026, 3, 2)), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(new DateTime(2026, 3, 3)), Times.Exactly(2));
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(new DateTime(2026, 3, 4)), Times.Once);
        VerifyNoOtherCalls();
    }
}
