using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class ActiveUserTokenCheckTests
{
    private readonly Mock<IUserAccessCheck> _accessCheck = new();
    private readonly CancellationToken _token = new CancellationTokenSource().Token;

    private void VerifyNoOtherCalls()
    {
        _accessCheck.VerifyNoOtherCalls();
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "Bearer"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsActiveAsync_ShouldFollowTheAccessCheck_ForTheTokenSubject(bool canSignIn)
    {
        _accessCheck.Setup(x => x.CanSignInAsync("user-1", _token)).ReturnsAsync(canSignIn);

        var result = await ActiveUserTokenCheck.IsActiveAsync(Principal(new Claim(ClaimTypes.NameIdentifier, "user-1")), _accessCheck.Object, _token);

        result.Should().Be(canSignIn);
        _accessCheck.Verify(x => x.CanSignInAsync("user-1", _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsActiveAsync_ShouldFollowTheSession_OfATokenThatCarriesOne(bool sessionActive)
    {
        var sessionId = Guid.NewGuid();
        _accessCheck.Setup(x => x.CanSignInAsync("user-1", _token)).ReturnsAsync(true);
        _accessCheck.Setup(x => x.IsSessionActiveAsync(sessionId, _token)).ReturnsAsync(sessionActive);
        var principal = Principal(new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim(TokenService.SessionIdClaim, sessionId.ToString()));

        var result = await ActiveUserTokenCheck.IsActiveAsync(principal, _accessCheck.Object, _token);

        result.Should().Be(sessionActive);
        _accessCheck.Verify(x => x.CanSignInAsync("user-1", _token), Times.Once);
        _accessCheck.Verify(x => x.IsSessionActiveAsync(sessionId, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsActiveAsync_ShouldRefuseATokenWithAnUnreadableSession()
    {
        _accessCheck.Setup(x => x.CanSignInAsync("user-1", _token)).ReturnsAsync(true);
        var principal = Principal(new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim(TokenService.SessionIdClaim, "not-a-guid"));

        (await ActiveUserTokenCheck.IsActiveAsync(principal, _accessCheck.Object, _token)).Should().BeFalse();

        _accessCheck.Verify(x => x.CanSignInAsync("user-1", _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsActiveAsync_ShouldRefuseATokenWithoutASubject()
    {
        (await ActiveUserTokenCheck.IsActiveAsync(Principal(), _accessCheck.Object, _token)).Should().BeFalse();
        (await ActiveUserTokenCheck.IsActiveAsync(null, _accessCheck.Object, _token)).Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsActiveAsync_ShouldRefuseATokenWithAnEmptySubject()
    {
        var principal = Principal(new Claim(ClaimTypes.NameIdentifier, ""));

        (await ActiveUserTokenCheck.IsActiveAsync(principal, _accessCheck.Object, _token)).Should().BeFalse();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsActiveAsync_ShouldNotAskForTheSession_WhenTheUserCannotSignIn()
    {
        _accessCheck.Setup(x => x.CanSignInAsync("user-1", _token)).ReturnsAsync(false);
        var principal = Principal(new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim(TokenService.SessionIdClaim, Guid.NewGuid().ToString()));

        (await ActiveUserTokenCheck.IsActiveAsync(principal, _accessCheck.Object, _token)).Should().BeFalse();

        _accessCheck.Verify(x => x.CanSignInAsync("user-1", _token), Times.Once);
        VerifyNoOtherCalls();
    }
}
