using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Email.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class RequireOidcHttpsFilterTests
{
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock = new();

    private void VerifyNoOtherCalls()
    {
        _publicUrlBuilderMock.VerifyNoOtherCalls();
    }

    private static ActionExecutingContext Context(string scheme, string host, params (string Name, string Value)[] headers)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = scheme;
        httpContext.Request.Host = new HostString(host);
        foreach (var (name, value) in headers)
        {
            httpContext.Request.Headers[name] = value;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), new object());
    }

    private async Task<(ActionExecutingContext Context, bool ActionRan)> RunAsync(OidcHttpsRefusal refusal, ActionExecutingContext context)
    {
        var filter = new RequireOidcHttpsFilter(refusal, _publicUrlBuilderMock.Object, Mock.Of<ILogger<RequireOidcHttpsFilter>>());
        var actionRan = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            actionRan = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });

        return (context, actionRan);
    }

    [Theory]
    [InlineData("https", "games.example.com", null)]
    [InlineData("https", "192.168.1.20:5444", "https://games.example.com")]
    [InlineData("http", "localhost:5444", null)]
    [InlineData("http", "127.0.0.1:5444", null)]
    [InlineData("http", "[::1]:5444", null)]
    public async Task OnActionExecutionAsync_ShouldRunTheAction_OverHttpsOrLoopback(string scheme, string host, string? publicUrl)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);

        var (context, actionRan) = await RunAsync(OidcHttpsRefusal.LoginPage, Context(scheme, host));

        actionRan.Should().BeTrue();
        context.Result.Should().BeNull();
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(OidcHttpsRefusal.LoginPage, "/login?error=error.auth.oidc-https-required")]
    [InlineData(OidcHttpsRefusal.CallbackPage, "/auth-callback?error=error.auth.oidc-https-required")]
    public async Task OnActionExecutionAsync_ShouldRedirectToTheRefusalPage_OverPlainHttp(OidcHttpsRefusal refusal, string expected)
    {
        var (context, actionRan) = await RunAsync(refusal, Context("http", "192.168.1.20:5444"));

        actionRan.Should().BeFalse();
        context.Result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be(expected);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldThrowTheHttpsKey_ForABadRequestRefusal()
    {
        var act = () => RunAsync(OidcHttpsRefusal.BadRequest, Context("http", "192.168.1.20:5444"));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Be(Constants.Errors.OidcHttpsRequired);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http://games.example.com")]
    [InlineData("http://192.168.1.20:5444")]
    public async Task OnActionExecutionAsync_ShouldRefuse_WhenThePublicUrlIsPlainHttp_EvenOverHttps(string publicUrl)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);

        var (context, actionRan) = await RunAsync(OidcHttpsRefusal.CallbackPage, Context("https", "games.example.com"));

        actionRan.Should().BeFalse();
        context.Result.Should().BeOfType<LocalRedirectResult>();
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldIgnoreASpoofedForwardedProto_WithoutATrustedProxy()
    {
        var context = Context("http", "games.example.com", ("X-Forwarded-Proto", "https"), ("X-Forwarded-Host", "localhost"), ("X-Forwarded-For", "127.0.0.1"));

        var (_, actionRan) = await RunAsync(OidcHttpsRefusal.LoginPage, context);

        actionRan.Should().BeFalse();
        context.Result.Should().BeOfType<LocalRedirectResult>();
        VerifyNoOtherCalls();
    }
}
