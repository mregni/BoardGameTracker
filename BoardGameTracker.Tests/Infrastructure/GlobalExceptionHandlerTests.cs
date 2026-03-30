using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
    }

    private static (HttpContext httpContext, MemoryStream responseBody) CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        return (httpContext, responseBody);
    }

    private static async Task<ProblemDetails> GetProblemDetailsFromResponse(MemoryStream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(responseBody);
        return problemDetails!;
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationException_ShouldReturn400WithExceptionMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ValidationException("Validation failed");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Validation failed");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationExceptionWithFieldAndError_ShouldReturn400WithExceptionMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ValidationException("Email", "Email is invalid");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Email is invalid");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationExceptionWithDictionary_ShouldReturn400WithDefaultMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            { "Name", ["Name is required"]}
        };
        var exception = new ValidationException(errors);

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("One or more validation errors occurred.");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithDomainException_ShouldReturn400WithExceptionMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new DomainException("Domain rule violated");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Domain rule violated");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithDomainExceptionWithErrorCode_ShouldReturn400WithExceptionMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new DomainException("CUSTOM_ERROR", "Custom domain error");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Custom domain error");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithEntityNotFoundException_ShouldReturn404WithExceptionMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new EntityNotFoundException("Game", 123);

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(404);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Game with ID '123' was not found.");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithEntityNotFoundExceptionWithCustomMessage_ShouldReturn404WithCustomMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new EntityNotFoundException("Player", 456, "The specified player does not exist");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(404);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("The specified player does not exist");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_ShouldReturn404WithExceptionMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new KeyNotFoundException("The key was not found in the dictionary");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(404);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("The key was not found in the dictionary");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentException_ShouldReturn400WithExceptionMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ArgumentException("Invalid argument provided");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Invalid argument provided");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithGenericException_ShouldReturn500WithGenericMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new Exception("This is a sensitive internal error message");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(500);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("An unexpected error occurred. Please try again later.");
        problemDetails.Title.Should().NotBe("This is a sensitive internal error message");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithNullReferenceException_ShouldReturn500WithGenericMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new NullReferenceException("Object reference not set to an instance of an object");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(500);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("An unexpected error occurred. Please try again later.");
        problemDetails.Title.Should().NotContain("Object reference not set");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithInvalidOperationException_ShouldReturn500WithGenericMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new InvalidOperationException("Invalid operation occurred");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(500);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("An unexpected error occurred. Please try again later.");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationException_ShouldNotLog()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ValidationException("Validation failed");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithDomainException_ShouldNotLog()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new DomainException("Domain error");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithEntityNotFoundException_ShouldNotLog()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new EntityNotFoundException("Game", 1);

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_ShouldNotLog()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new KeyNotFoundException("Key not found");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentException_ShouldNotLog()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ArgumentException("Invalid argument");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldAlwaysReturnTrue()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new Exception("Test exception");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Validation error 1")]
    [InlineData("Validation error 2")]
    [InlineData("Different validation message")]
    public async Task TryHandleAsync_WithValidationException_ShouldHandleVariousMessages(string message)
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ValidationException(message);

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Title.Should().Be(message);

        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Game", 1)]
    [InlineData("Player", 42)]
    [InlineData("Session", 999)]
    public async Task TryHandleAsync_WithEntityNotFoundException_ShouldHandleVariousEntities(string entityType, int entityId)
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new EntityNotFoundException(entityType, entityId);

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(404);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Title.Should().Be($"{entityType} with ID '{entityId}' was not found.");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithCancellationToken_ShouldPassThroughCancellationToken()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ValidationException("Test");
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var result = await _handler.TryHandleAsync(httpContext, exception, cancellationToken);

        result.Should().BeTrue();
        VerifyNoOtherCalls();
    }
}
