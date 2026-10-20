using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class ActiveUserTokenCheckTests
{
    private readonly Mock<IUserAccessCheck> _accessCheck = new();

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "Bearer"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsActiveAsync_ShouldFollowTheAccessCheck_ForTheTokenSubject(bool canSignIn)
    {
        _accessCheck.Setup(x => x.CanSignInAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(canSignIn);

        var result = await ActiveUserTokenCheck.IsActiveAsync(Principal(new Claim(ClaimTypes.NameIdentifier, "user-1")), _accessCheck.Object, TestContext.Current.CancellationToken);

        result.Should().Be(canSignIn);
        _accessCheck.Verify(x => x.CanSignInAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _accessCheck.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsActiveAsync_ShouldRefuseATokenWithoutASubject()
    {
        (await ActiveUserTokenCheck.IsActiveAsync(Principal(), _accessCheck.Object, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await ActiveUserTokenCheck.IsActiveAsync(null, _accessCheck.Object, TestContext.Current.CancellationToken)).Should().BeFalse();
        _accessCheck.VerifyNoOtherCalls();
    }
}
