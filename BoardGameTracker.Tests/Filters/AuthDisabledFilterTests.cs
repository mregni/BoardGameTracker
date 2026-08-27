using System.Collections.Generic;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Configuration.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Filters;

public class AuthDisabledFilterTests
{
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly AuthDisabledFilter _filter;

    public AuthDisabledFilterTests()
    {
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _filter = new AuthDisabledFilter(_environmentProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    private static ActionExecutingContext CreateContext(string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = new PathString(path);

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    [Fact]
    public void OnActionExecuting_ShouldNotSetResult_WhenAuthIsEnabled()
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);
        var context = CreateContext("/api/games");

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void OnActionExecuting_ShouldSetConflictResult_WhenAuthIsDisabledAndPathIsEmpty()
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var context = CreateContext("/");

        _filter.OnActionExecuting(context);

        var conflictResult = context.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().Be("Authentication is disabled. This endpoint is not available.");
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("/api/auth/status")]
    [InlineData("/api/auth/STATUS")]
    [InlineData("/api/auth/Status")]
    [InlineData("/status")]
    public void OnActionExecuting_ShouldNotSetResult_WhenAuthIsDisabledAndPathEndsWithStatusCaseInsensitive(string path)
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var context = CreateContext(path);

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("/api/games")]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/register")]
    [InlineData("/api/settings")]
    [InlineData("/mystatus")]
    [InlineData("/api/auth/mystatus")]
    public void OnActionExecuting_ShouldSetConflictResult_WhenAuthIsDisabledAndVariousNonStatusPaths(string path)
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var context = CreateContext(path);

        _filter.OnActionExecuting(context);

        var conflictResult = context.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().Be("Authentication is disabled. This endpoint is not available.");
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }
}
