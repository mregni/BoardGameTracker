using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class PlayerLinkGuardTests : IDisposable
{
    private const int PlayerId = 7;

    private readonly MainDbContext _context = new(new DbContextOptionsBuilder<MainDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<ApplicationUser> AddUserAsync(string name, int? playerId)
    {
        var user = new ApplicationUser(name, $"{name}@example.com");
        if (playerId.HasValue)
        {
            user.LinkPlayer(playerId.Value);
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private async Task AddPlayerAsync()
    {
        _context.Players.Add(new Player("Jane") { Id = PlayerId });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EnsureLinkableAsync_ShouldThrowNotFound_WhenThePlayerDoesNotExist()
    {
        var act = () => PlayerLinkGuard.EnsureLinkableAsync(_context, PlayerId, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task EnsureLinkableAsync_ShouldThrow_WhenAnotherUserHoldsThePlayer()
    {
        await AddPlayerAsync();
        var self = await AddUserAsync("self", null);
        await AddUserAsync("other", PlayerId);

        var act = () => PlayerLinkGuard.EnsureLinkableAsync(_context, PlayerId, self.Id);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PlayerAlreadyLinked);
    }

    [Fact]
    public async Task EnsureLinkableAsync_ShouldPass_WhenTheExcludedUserHoldsThePlayer()
    {
        await AddPlayerAsync();
        var self = await AddUserAsync("self", PlayerId);

        var act = () => PlayerLinkGuard.EnsureLinkableAsync(_context, PlayerId, self.Id);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureLinkableAsync_ShouldPass_WhenOtherUsersHoldOtherPlayers()
    {
        await AddPlayerAsync();
        var self = await AddUserAsync("self", null);
        await AddUserAsync("unlinked", null);
        await AddUserAsync("linked-elsewhere", PlayerId + 1);

        var act = () => PlayerLinkGuard.EnsureLinkableAsync(_context, PlayerId, self.Id);

        await act.Should().NotThrowAsync();
    }
}
