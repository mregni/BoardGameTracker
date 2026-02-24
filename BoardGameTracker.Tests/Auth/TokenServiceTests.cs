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
        var roles = new List<string> { "Admin", "Reader" };

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
        var roles = new List<string> { "Admin", "Reader" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList();
        roleClaims.Should().HaveCount(2);
        roleClaims.Select(c => c.Value).Should().Contain("Admin").And.Contain("Reader");
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

        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken.Token);
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
    public async Task RevokeRefreshTokenAsync_ShouldRevokeToken()
    {
        // Arrange
        var userId = "test-user-id";
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(userId);

        // Act
        await _tokenService.RevokeRefreshTokenAsync(refreshToken, "Test revocation", "new-token");

        // Assert
        var stored = await _context.RefreshTokens.FirstAsync(t => t.Token == refreshToken.Token);
        stored.IsRevoked.Should().BeTrue();
        stored.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllActiveTokens()
    {
        // Arrange
        var userId = "test-user-id";
        await _tokenService.GenerateRefreshTokenAsync(userId);
        await _tokenService.GenerateRefreshTokenAsync(userId);
        await _tokenService.GenerateRefreshTokenAsync(userId);

        // Act
        await _tokenService.RevokeAllUserTokensAsync(userId, "Bulk revocation");

        // Assert
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
        tokens.Should().HaveCount(3);
        tokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public void GetAccessTokenExpiry_ShouldReturnFutureDate()
    {
        // Act
        var expiry = _tokenService.GetAccessTokenExpiry();

        // Assert
        expiry.Should().BeAfter(DateTime.UtcNow);
        expiry.Should().BeBefore(DateTime.UtcNow.AddMinutes(61));
    }
}
