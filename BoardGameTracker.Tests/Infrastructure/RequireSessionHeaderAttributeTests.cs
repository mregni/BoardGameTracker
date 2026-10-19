using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class RequireSessionHeaderAttributeTests
{
    private readonly RequireSessionHeaderAttribute _filter = new();

    private static ActionExecutingContext CreateContext(string? headerValue)
    {
        var httpContext = new DefaultHttpContext();
        if (headerValue != null)
        {
            httpContext.Request.Headers[RequireSessionHeaderAttribute.HeaderName] = headerValue;
        }

        return new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [],
            new Dictionary<string, object?>(),
            new object());
    }

    [Fact]
    public void OnActionExecuting_ShouldLetTheRequestThrough_WhenTheHeaderIsPresent()
    {
        var context = CreateContext(RequireSessionHeaderAttribute.HeaderValue);

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xmlhttprequest")]
    [InlineData("fetch")]
    public void OnActionExecuting_ShouldReturn403_WhenTheHeaderIsMissingOrWrong(string? headerValue)
    {
        var context = CreateContext(headerValue);

        _filter.OnActionExecuting(context);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.Value.Should().BeOfType<ProblemDetails>().Which.Title.Should().Be(Constants.Errors.MissingSessionHeader);
    }
}
