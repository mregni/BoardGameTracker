using System.Collections.Generic;
using BoardGameTracker.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Filters;

public class ValidateIdFilterTests
{
    private readonly ValidateIdFilter _filter;

    public ValidateIdFilterTests()
    {
        _filter = new ValidateIdFilter();
    }

    private static ActionExecutingContext CreateContext(Dictionary<string, object?> actionArguments)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments,
            new object());
    }

    [Fact]
    public void OnActionExecuting_ShouldSetBadRequestResult_WhenIdParameterIsZero()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "id", 0 } });

        _filter.OnActionExecuting(context);

        var badRequestResult = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObject = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObject.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObject).Should().Be("Invalid id. Must be greater than 0.");
    }

    [Fact]
    public void OnActionExecuting_ShouldSetBadRequestResult_WhenIdParameterIsNegative()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "id", -5 } });

        _filter.OnActionExecuting(context);

        var badRequestResult = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObject = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObject.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObject).Should().Be("Invalid id. Must be greater than 0.");
    }

    [Fact]
    public void OnActionExecuting_ShouldNotSetResult_WhenIdParameterIsPositive()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "id", 1 } });

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_ShouldSetBadRequestResult_WhenPlayerIdParameterIsZero()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "playerId", 0 } });

        _filter.OnActionExecuting(context);

        var badRequestResult = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObject = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObject.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObject).Should().Be("Invalid playerId. Must be greater than 0.");
    }

    [Fact]
    public void OnActionExecuting_ShouldNotSetResult_WhenGameNightIdParameterIsPositive()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "gameNightId", 42 } });

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_ShouldSetBadRequestResult_WhenIdParameterIsUppercaseAndZero()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "Id", 0 } });

        _filter.OnActionExecuting(context);

        var badRequestResult = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObject = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObject.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObject).Should().Be("Invalid Id. Must be greater than 0.");
    }

    [Fact]
    public void OnActionExecuting_ShouldNotSetResult_WhenCountParameterIsZero()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "count", 0 } });

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_ShouldNotSetResult_WhenParameterIsString()
    {
        var context = CreateContext(new Dictionary<string, object?> { { "name", "testname" } });

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_ShouldSetBadRequestResult_WhenOneOfMultipleParametersIsInvalid()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "name", "test" },
            { "count", 5 },
            { "gameId", 0 },
            { "limit", 10 }
        });

        _filter.OnActionExecuting(context);

        var badRequestResult = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObject = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObject.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObject).Should().Be("Invalid gameId. Must be greater than 0.");
    }

    [Fact]
    public void OnActionExecuting_ShouldNotSetResult_WhenNoParametersProvided()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuted_ShouldDoNothing()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object());

        _filter.OnActionExecuted(context);

        context.Result.Should().BeNull();
    }

    [Theory]
    [InlineData("sessionId", -1)]
    [InlineData("locationId", 0)]
    [InlineData("badgeId", -100)]
    [InlineData("loanId", 0)]
    public void OnActionExecuting_ShouldSetBadRequestResult_WhenVariousIdParametersAreInvalid(string parameterName, int value)
    {
        var context = CreateContext(new Dictionary<string, object?> { { parameterName, value } });

        _filter.OnActionExecuting(context);

        var badRequestResult = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObject = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObject.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObject).Should().Be($"Invalid {parameterName}. Must be greater than 0.");
    }

    [Theory]
    [InlineData("sessionId", 1)]
    [InlineData("locationId", 50)]
    [InlineData("badgeId", 999)]
    [InlineData("loanId", 42)]
    public void OnActionExecuting_ShouldNotSetResult_WhenVariousIdParametersAreValid(string parameterName, int value)
    {
        var context = CreateContext(new Dictionary<string, object?> { { parameterName, value } });

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Theory]
    [InlineData("ID", 0)]
    [InlineData("iD", -5)]
    [InlineData("GameID", 0)]
    [InlineData("PLAYERID", -10)]
    public void OnActionExecuting_ShouldSetBadRequestResult_WhenIdParametersHaveMixedCase(string parameterName, int value)
    {
        var context = CreateContext(new Dictionary<string, object?> { { parameterName, value } });

        _filter.OnActionExecuting(context);

        var badRequestResult = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObject = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObject.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObject).Should().Be($"Invalid {parameterName}. Must be greater than 0.");
    }

    [Theory]
    [InlineData("count", -1)]
    [InlineData("limit", 0)]
    [InlineData("offset", -5)]
    [InlineData("page", 0)]
    public void OnActionExecuting_ShouldNotSetResult_WhenNonIdIntParametersAreInvalid(string parameterName, int value)
    {
        var context = CreateContext(new Dictionary<string, object?> { { parameterName, value } });

        _filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }
}
