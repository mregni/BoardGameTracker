using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class TokenServiceTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-that-is-at-least-32-characters-long",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();

        _tokenService = new TokenService(config, _context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        // Arrange
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var roles = new List<string> { "Admin", "User" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be("test-issuer");
        jwtToken.Audiences.Should().Contain("test-audience");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "testuser");
        jwtToken.Claims.Should().Contain(c => c.Type == "display_name" && c.Value == "Test User");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeRoleClaims()
    {
        // Arrange
        var user = new ApplicationUser("testuser", "test@test.com");
        var roles = new List<string> { "Admin", "User" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList();
        roleClaims.Should().HaveCount(2);
        roleClaims.Select(c => c.Value).Should().Contain("Admin").And.Contain("User");
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldCreateAndStoreToken()
    {
        // Arrange
        var userId = "test-user-id";

        // Act
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(userId);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.Token.Should().NotBeNullOrEmpty();
        refreshToken.UserId.Should().Be(userId);
        refreshToken.IsActive.Should().BeTrue();
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsRevoked.Should().BeFalse();

        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken.Token, TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _tokenService.GetRefreshTokenAsync("nonexistent-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldRevokeTokenWithReasonAndReplacement()
    {
        var userId = "test-user-id";
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(userId);

        await _tokenService.RevokeRefreshTokenAsync(refreshToken, "Test revocation", "new-token");

        var stored = await _context.RefreshTokens.FirstAsync(t => t.Token == refreshToken.Token, TestContext.Current.CancellationToken);
        stored.IsRevoked.Should().BeTrue();
        stored.IsActive.Should().BeFalse();
        stored.RevokedReason.Should().Be("Test revocation");
        stored.ReplacedByToken.Should().Be("new-token");
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeOnlyActiveTokensOfThatUser()
    {
        var userId = "test-user-id";
        await _tokenService.GenerateRefreshTokenAsync(userId);
        await _tokenService.GenerateRefreshTokenAsync(userId);
        var preRevoked = await _tokenService.GenerateRefreshTokenAsync(userId);
        await _tokenService.RevokeRefreshTokenAsync(preRevoked, "Original reason");
        var otherUsersToken = await _tokenService.GenerateRefreshTokenAsync("other-user-id");

        await _tokenService.RevokeAllUserTokensAsync(userId, "Bulk revocation");

        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(TestContext.Current.CancellationToken);
        tokens.Should().HaveCount(3);
        tokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
        tokens.Where(t => t.Token != preRevoked.Token)
            .Should().AllSatisfy(t => t.RevokedReason.Should().Be("Bulk revocation"));

        var storedPreRevoked = await _context.RefreshTokens.FirstAsync(t => t.Token == preRevoked.Token, TestContext.Current.CancellationToken);
        storedPreRevoked.RevokedReason.Should().Be("Original reason");

        var storedOther = await _context.RefreshTokens.FirstAsync(t => t.Token == otherUsersToken.Token, TestContext.Current.CancellationToken);
        storedOther.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ShouldReturnToken_WhenExists()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var created = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        var result = await _tokenService.GetRefreshTokenAsync(created.Token);

        result.Should().NotBeNull();
        result!.Token.Should().Be(created.Token);
        result.UserId.Should().Be(user.Id);
        result.User.Should().NotBeNull();
    }

    [Fact]
    public void GenerateAccessToken_ShouldOmitDisplayNameClaim_WhenDisplayNameIsNull()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        var roles = new List<string> { "User" };

        var token = _tokenService.GenerateAccessToken(user, roles);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().NotContain(c => c.Type == "display_name");
    }

    [Fact]
    public void GenerateAccessToken_ShouldThrowInvalidOperationException_WhenSecretIsMissing()
    {
        var originalSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        try
        {
            var config = new ConfigurationBuilder().Build();
            var service = new TokenService(config, _context);
            var user = new ApplicationUser("testuser", "test@test.com");

            var act = () => service.GenerateAccessToken(user, new List<string>());

            act.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", originalSecret);
        }
    }

    [Fact]
    public void GetAccessTokenExpiry_ShouldReturnConfiguredExpiry()
    {
        var expiry = _tokenService.GetAccessTokenExpiry();

        expiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldRemoveTokensExpiredOverThirtyDaysAgo()
    {
        var oldExpired = RefreshToken.Create("test-user-id", -31);
        var active = RefreshToken.Create("test-user-id", 7);
        _context.RefreshTokens.AddRange(oldExpired, active);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _tokenService.CleanupExpiredTokensAsync();

        var remaining = await _context.RefreshTokens.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().ContainSingle(t => t.Token == active.Token);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldRemoveTokensRevokedOverThirtyDaysAgo()
    {
        var oldRevoked = RefreshToken.Create("test-user-id", 90);
        oldRevoked.Revoke("Old revocation");
        typeof(RefreshToken).GetProperty(nameof(RefreshToken.RevokedAt))!
            .SetValue(oldRevoked, DateTime.UtcNow.AddDays(-31));
        var active = RefreshToken.Create("test-user-id", 7);
        _context.RefreshTokens.AddRange(oldRevoked, active);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _tokenService.CleanupExpiredTokensAsync();

        var remaining = await _context.RefreshTokens.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().ContainSingle(t => t.Token == active.Token);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldKeepRecentlyExpiredAndRecentlyRevokedTokens()
    {
        var recentlyExpired = RefreshToken.Create("test-user-id", -1);
        var recentlyRevoked = RefreshToken.Create("test-user-id", 7);
        recentlyRevoked.Revoke("Recent revocation");
        _context.RefreshTokens.AddRange(recentlyExpired, recentlyRevoked);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _tokenService.CleanupExpiredTokensAsync();

        var remaining = await _context.RefreshTokens.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().HaveCount(2);
    }
}
