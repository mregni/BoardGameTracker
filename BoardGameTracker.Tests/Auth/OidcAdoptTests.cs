using System.Text.Json;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcAdoptTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private readonly Mock<IOidcService> _oidcServiceMock = new();
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock = new();
    private readonly Mock<IProfileImageTicketService> _imageTicketsMock = new();
    private readonly OidcController _controller;

    public OidcAdoptTests()
    {
        _imageTicketsMock.Setup(x => x.Issue()).Returns("image-ticket");
        _imageTicketsMock.Setup(x => x.Lifetime).Returns(TimeSpan.FromDays(7));
        var sessionCookies = new AuthSessionCookies(_imageTicketsMock.Object, Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }));
        _controller = new OidcController(_oidcServiceMock.Object, _publicUrlBuilderMock.Object, sessionCookies, Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }), Mock.Of<ILogger<OidcController>>());
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{OidcController.HandoffCookieName}=handoff-key";
        _controller.ControllerContext = new ControllerContext { HttpContext = context };
    }

    private void VerifyNoOtherCalls()
    {
        _oidcServiceMock.VerifyNoOtherCalls();
        _publicUrlBuilderMock.VerifyNoOtherCalls();
    }

    private string[] SetCookies => _controller.HttpContext.Response.Headers.SetCookie.Select(c => c!).ToArray();

    [Fact]
    public void Adopt_ShouldMoveTheRefreshTokenIntoTheCookie_AndKeepItOutOfTheBody()
    {
        var login = new LoginResponse("access", "refresh-secret", DateTime.UtcNow.AddHours(1), new UserInfo("id", "jane", "Jane", ["User"]));
        _oidcServiceMock.Setup(x => x.TakeHandoff("handoff-key")).Returns(login);

        var result = _controller.Adopt();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(login);
        var body = JsonSerializer.Serialize(ok.Value, WebJson);
        body.Should().Contain("\"accessToken\":\"access\"").And.NotContain("refresh-secret").And.NotContainEquivalentOf("refreshToken");
        SetCookies.Should().Contain(c => c.StartsWith($"{AuthSessionCookies.RefreshCookieName}=refresh-secret;", StringComparison.Ordinal)
                                          && c.Contains("path=/api/auth;") && c.Contains("httponly") && c.Contains("samesite=lax"));
        SetCookies.Should().Contain(c => c.StartsWith($"{OidcController.HandoffCookieName}=;", StringComparison.Ordinal));
        SetCookies.Should().Contain(c => c.StartsWith($"{ProfileImageCookie.Name}=image-ticket", StringComparison.Ordinal));

        _oidcServiceMock.Verify(x => x.TakeHandoff("handoff-key"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Adopt_ShouldRefuse_WithoutSettingASession_WhenTheHandoffIsGone()
    {
        _oidcServiceMock.Setup(x => x.TakeHandoff("handoff-key")).Returns((LoginResponse?)null);

        var act = () => _controller.Adopt();

        act.Should().Throw<AuthenticationFailedException>().WithMessage(Constants.Errors.OidcHandoffExpired);
        SetCookies.Should().NotContain(c => c.StartsWith($"{AuthSessionCookies.RefreshCookieName}=", StringComparison.Ordinal));

        _oidcServiceMock.Verify(x => x.TakeHandoff("handoff-key"), Times.Once);
        VerifyNoOtherCalls();
    }
}
