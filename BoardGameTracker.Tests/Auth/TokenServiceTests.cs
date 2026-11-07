using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        });

        _tokenService = new TokenService(jwtOptions, _context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var roles = new List<string> { "Admin", "User" };

        var token = _tokenService.GenerateAccessToken(user, roles, Guid.NewGuid());

        var jwtToken = new JsonWebTokenHandler().ReadJsonWebToken(token);
        jwtToken.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
        jwtToken.Issuer.Should().Be("test-issuer");
        jwtToken.Audiences.Should().Equal("test-audience");
        jwtToken.Subject.Should().Be(user.Id);
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "testuser");
        jwtToken.Claims.Should().Contain(c => c.Type == "display_name" && c.Value == "Test User");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeRoleClaims()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        var roles = new List<string> { "Admin", "User" };

        var token = _tokenService.GenerateAccessToken(user, roles, Guid.NewGuid());

        var jwtToken = new JsonWebTokenHandler().ReadJsonWebToken(token);
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList();
        roleClaims.Select(c => c.Value).Should().BeEquivalentTo("Admin", "User");
    }

    [Fact]
    public void GenerateAccessToken_ShouldCarryTheSession()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        var sessionId = Guid.NewGuid();

        var token = _tokenService.GenerateAccessToken(user, new List<string>(), sessionId);

        new JsonWebTokenHandler().ReadJsonWebToken(token).Claims.Should().Contain(c => c.Type == TokenService.SessionIdClaim && c.Value == sessionId.ToString());
    }

    [Fact]
    public async Task ReplacingARefreshToken_ShouldKeepItsSession_WhileANewLoginGetsItsOwn()
    {
        var first = await _tokenService.GenerateRefreshTokenAsync("user-1");
        var rotated = await _tokenService.RotateRefreshTokenAsync(first);
        var superseded = await _tokenService.SupersedeRefreshTokenAsync(rotated);
        var otherDevice = await _tokenService.GenerateRefreshTokenAsync("user-1");

        rotated.SessionId.Should().Be(first.SessionId);
        superseded.SessionId.Should().Be(first.SessionId);
        otherDevice.SessionId.Should().NotBe(first.SessionId);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldCreateAndStoreToken()
    {
        var userId = "test-user-id";

        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(userId);

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
        var result = await _tokenService.GetRefreshTokenAsync("nonexistent-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldRevokeTheTokenWithItsReason()
    {
        var userId = "test-user-id";
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(userId);

        await _tokenService.RevokeRefreshTokenAsync(refreshToken, "Test revocation");

        var stored = await _context.RefreshTokens.AsNoTracking().FirstAsync(t => t.Token == refreshToken.Token, TestContext.Current.CancellationToken);
        stored.IsRevoked.Should().BeTrue();
        stored.IsActive.Should().BeFalse();
        stored.RevokedReason.Should().Be("Test revocation");
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldAlsoRevokeTheTokensItWasRotatedInto_AndKeepTheChain()
    {
        var first = await _tokenService.GenerateRefreshTokenAsync("test-user-id");
        var second = await _tokenService.RotateRefreshTokenAsync(first);
        var third = await _tokenService.RotateRefreshTokenAsync(second);

        await _tokenService.RevokeRefreshTokenAsync(first, "Logged out");

        var stored = await _context.RefreshTokens.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        stored.Should().AllSatisfy(t => t.IsActive.Should().BeFalse());
        stored.Single(t => t.Token == first.Token).ReplacedByToken.Should().Be(second.Token);
        stored.Single(t => t.Token == first.Token).RevokedReason.Should().Be("Replaced by new token");
        stored.Single(t => t.Token == third.Token).RevokedReason.Should().Be("Logged out");
    }

    [Fact]
    public async Task SupersedeRefreshTokenAsync_ShouldReplaceAnUnusedToken_WithoutMarkingItUsed()
    {
        var first = await _tokenService.GenerateRefreshTokenAsync("test-user-id");
        var second = await _tokenService.RotateRefreshTokenAsync(first);

        var third = await _tokenService.SupersedeRefreshTokenAsync(second);

        var stored = await _context.RefreshTokens.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        var storedFirst = stored.Single(t => t.Token == first.Token);
        var storedSecond = stored.Single(t => t.Token == second.Token);
        storedFirst.WasUsed.Should().BeTrue();
        storedFirst.IsSuperseded.Should().BeFalse();
        storedSecond.WasUsed.Should().BeFalse();
        storedSecond.IsSuperseded.Should().BeTrue();
        storedSecond.ReplacedByToken.Should().Be(third.Token);
        storedSecond.RevokedReason.Should().Be(RefreshToken.SupersededReason);
        stored.Single(t => t.Token == third.Token).IsActive.Should().BeTrue();
        third.PlainTextToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSuccessorAsync_ShouldReturnTheTokenItWasRotatedInto()
    {
        var first = await _tokenService.GenerateRefreshTokenAsync("user-1");
        var second = await _tokenService.RotateRefreshTokenAsync(first);

        var successor = await _tokenService.GetSuccessorAsync(first);

        successor!.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task GetSuccessorAsync_ShouldReturnNull_WhenTheTokenWasNeverRotated()
    {
        var token = await _tokenService.GenerateRefreshTokenAsync("user-1");

        var successor = await _tokenService.GetSuccessorAsync(token);

        successor.Should().BeNull();
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ShouldRefuseTheSecondOfTwoConcurrentRotations()
    {
        var databaseName = Guid.NewGuid().ToString();
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "this-is-a-very-long-secret-key-for-testing-purposes-at-least-32-chars",
            Issuer = "BoardGameTracker",
            Audience = "BoardGameTracker",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
        });
        await using var setup = NewContext(databaseName);
        var original = await new TokenService(jwtOptions, setup).GenerateRefreshTokenAsync("test-user-id");
        await using var first = NewContext(databaseName);
        await using var second = NewContext(databaseName);
        var firstCopy = await first.RefreshTokens.SingleAsync(t => t.Token == original.Token, TestContext.Current.CancellationToken);
        var secondCopy = await second.RefreshTokens.SingleAsync(t => t.Token == original.Token, TestContext.Current.CancellationToken);

        await new TokenService(jwtOptions, first).RotateRefreshTokenAsync(firstCopy);
        var act = () => new TokenService(jwtOptions, second).RotateRefreshTokenAsync(secondCopy);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    private static MainDbContext NewContext(string databaseName) =>
        new(new DbContextOptionsBuilder<MainDbContext>().UseInMemoryDatabase(databaseName).Options);

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

        var tokens = await _context.RefreshTokens.AsNoTracking().Where(t => t.UserId == userId).ToListAsync(TestContext.Current.CancellationToken);
        tokens.Should().HaveCount(3);
        tokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
        tokens.Where(t => t.Token != preRevoked.Token)
            .Should().AllSatisfy(t => t.RevokedReason.Should().Be("Bulk revocation"));

        var storedPreRevoked = await _context.RefreshTokens.AsNoTracking().FirstAsync(t => t.Token == preRevoked.Token, TestContext.Current.CancellationToken);
        storedPreRevoked.RevokedReason.Should().Be("Original reason");

        var storedOther = await _context.RefreshTokens.AsNoTracking().FirstAsync(t => t.Token == otherUsersToken.Token, TestContext.Current.CancellationToken);
        storedOther.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ShouldReturnToken_WhenExists()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var created = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        var result = await _tokenService.GetRefreshTokenAsync(created.PlainTextToken!);

        result!.Token.Should().Be(created.Token);
        result.UserId.Should().Be(user.Id);
        result.User.Should().BeSameAs(user);
    }

    [Fact]
    public void GenerateAccessToken_ShouldOmitDisplayNameClaim_WhenDisplayNameIsNull()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        var roles = new List<string> { "User" };

        var token = _tokenService.GenerateAccessToken(user, roles, Guid.NewGuid());

        var jwtToken = new JsonWebTokenHandler().ReadJsonWebToken(token);
        jwtToken.Claims.Should().NotContain(c => c.Type == "display_name");
    }

    [Fact]
    public void GenerateAccessToken_ShouldThrowInvalidOperationException_WhenSecretIsMissing()
    {
        var service = new TokenService(Options.Create(new JwtOptions()), _context);
        var user = new ApplicationUser("testuser", "test@test.com");

        var act = () => service.GenerateAccessToken(user, new List<string>(), Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("JWT Secret is not configured");
    }

    [Fact]
    public async Task GenerateAccessToken_ShouldSignWithTheConfiguredSecret()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        var token = _tokenService.GenerateAccessToken(user, new List<string>(), Guid.NewGuid());
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-that-is-at-least-32-characters-long")),
        };

        var valid = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);

        valid.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ShouldGiveEveryTokenItsOwnId()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        var sessionId = Guid.NewGuid();

        var first = new JsonWebTokenHandler().ReadJsonWebToken(_tokenService.GenerateAccessToken(user, new List<string>(), sessionId));
        var second = new JsonWebTokenHandler().ReadJsonWebToken(_tokenService.GenerateAccessToken(user, new List<string>(), sessionId));

        first.Id.Should().NotBeNullOrEmpty().And.NotBe(second.Id);
    }

    [Fact]
    public void Hash_ShouldBeUppercaseHexSha256()
    {
        RefreshToken.Hash("abc").Should().Be("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldStoreOnlyTheHash_AndExpireAfterTheConfiguredDays()
    {
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync("test-user-id");

        var stored = await _context.RefreshTokens.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        stored.Token.Should().Be(RefreshToken.Hash(refreshToken.PlainTextToken!));
        stored.Token.Should().NotBe(refreshToken.PlainTextToken);
        stored.PlainTextToken.Should().BeNull();
        stored.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ShouldNotAcceptTheStoredHashAsAToken()
    {
        var created = await _tokenService.GenerateRefreshTokenAsync("test-user-id");

        var result = await _tokenService.GetRefreshTokenAsync(created.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ShouldGiveTheReplacementTheConfiguredLifetime()
    {
        var current = await _tokenService.GenerateRefreshTokenAsync("test-user-id");

        var replacement = await _tokenService.RotateRefreshTokenAsync(current);

        var storedOld = await _context.RefreshTokens.AsNoTracking().FirstAsync(t => t.Token == current.Token, TestContext.Current.CancellationToken);
        storedOld.RevokedReason.Should().Be(RefreshToken.RotatedReason);
        storedOld.ReplacedByToken.Should().Be(replacement.Token);
        replacement.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldLeaveOtherSessionsAlone()
    {
        var token = await _tokenService.GenerateRefreshTokenAsync("test-user-id");
        var otherDevice = await _tokenService.GenerateRefreshTokenAsync("test-user-id");

        await _tokenService.RevokeRefreshTokenAsync(token, "Logged out");

        var stored = await _context.RefreshTokens.AsNoTracking().FirstAsync(t => t.Token == otherDevice.Token, TestContext.Current.CancellationToken);
        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldStop_WhenTheChainLoopsBackOnItself()
    {
        var first = RefreshToken.Create("test-user-id", 7);
        var second = RefreshToken.Create("test-user-id", 7);
        first.Revoke(RefreshToken.RotatedReason, second.Token);
        second.Revoke(RefreshToken.RotatedReason, first.Token);
        _context.RefreshTokens.AddRange(first, second);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _tokenService.RevokeRefreshTokenAsync(first, "Logged out").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldKeepTokensJustInsideTheThirtyDays_AndRemoveThoseJustOutside()
    {
        var keptExpired = RefreshToken.Create("test-user-id", -29);
        var goneExpired = RefreshToken.Create("test-user-id", -30);
        var keptRevoked = RevokedDaysAgo(29);
        var goneRevoked = RevokedDaysAgo(30);
        _context.RefreshTokens.AddRange(keptExpired, goneExpired, keptRevoked, goneRevoked);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _tokenService.CleanupExpiredTokensAsync();

        var remaining = await _context.RefreshTokens.AsNoTracking().Select(t => t.Token).ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().BeEquivalentTo(keptExpired.Token, keptRevoked.Token);
    }

    private static RefreshToken RevokedDaysAgo(int days)
    {
        var token = RefreshToken.Create("test-user-id", 90);
        token.Revoke("Old revocation");
        typeof(RefreshToken).GetProperty(nameof(RefreshToken.RevokedAt))!.SetValue(token, DateTime.UtcNow.AddDays(-days));
        return token;
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ShouldRevokeTheOldTokenAndStoreTheReplacementInOneSave()
    {
        var user = new ApplicationUser("rotate", "rotate@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var current = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        var replacement = await _tokenService.RotateRefreshTokenAsync(current);

        replacement.Token.Should().NotBe(current.Token);
        replacement.UserId.Should().Be(user.Id);
        replacement.IsActive.Should().BeTrue();
        var storedOld = await _context.RefreshTokens.AsNoTracking().FirstAsync(t => t.Token == current.Token, TestContext.Current.CancellationToken);
        storedOld.IsRevoked.Should().BeTrue();
        storedOld.ReplacedByToken.Should().Be(replacement.Token);
        (await _context.RefreshTokens.CountAsync(t => t.UserId == user.Id, TestContext.Current.CancellationToken)).Should().Be(2);
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

        var remaining = await _context.RefreshTokens.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
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

        var remaining = await _context.RefreshTokens.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
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

        var remaining = await _context.RefreshTokens.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().HaveCount(2);
    }
}
