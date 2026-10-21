using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class UserAccessCheckTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly UserAccessCheck _check;

    public UserAccessCheckTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);
        _check = new UserAccessCheck(_context, _cache);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<ApplicationUser> AddUserAsync(string id, DateTimeOffset? lockoutEnd = null)
    {
        var user = new ApplicationUser(id, $"{id}@example.com") { Id = id, LockoutEnd = lockoutEnd };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private async Task SetLockoutAsync(string id, DateTimeOffset? lockoutEnd)
    {
        var user = await _context.Users.SingleAsync(u => u.Id == id, TestContext.Current.CancellationToken);
        user.LockoutEnd = lockoutEnd;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CanSignInAsync_ShouldAllowAnActiveUser()
    {
        await AddUserAsync("active");

        (await _check.CanSignInAsync("active", TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task CanSignInAsync_ShouldAllowAUserWhoseLockoutHasEnded()
    {
        await AddUserAsync("returning", DateTimeOffset.UtcNow.AddMinutes(-1));

        (await _check.CanSignInAsync("returning", TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task CanSignInAsync_ShouldRefuseADisabledUser()
    {
        await AddUserAsync("disabled", DateTimeOffset.MaxValue);

        (await _check.CanSignInAsync("disabled", TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task CanSignInAsync_ShouldRefuseAUserThatNoLongerExists()
    {
        (await _check.CanSignInAsync("deleted", TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task CanSignInAsync_ShouldServeTheCachedAnswer_UntilItIsForgotten()
    {
        await AddUserAsync("user-1");
        (await _check.CanSignInAsync("user-1", TestContext.Current.CancellationToken)).Should().BeTrue();

        await SetLockoutAsync("user-1", DateTimeOffset.MaxValue);
        (await _check.CanSignInAsync("user-1", TestContext.Current.CancellationToken)).Should().BeTrue("the answer is cached briefly");

        _check.Forget("user-1");
        (await _check.CanSignInAsync("user-1", TestContext.Current.CancellationToken)).Should().BeFalse();

        await SetLockoutAsync("user-1", null);
        _check.Forget("user-1");
        (await _check.CanSignInAsync("user-1", TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public void CacheLifetime_ShouldBeShort()
    {
        UserAccessCheck.CacheLifetime.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(1));
    }
}
