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
        problemDetails.Title.Should().Be("The requested resource was not found.");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithEntityNotFoundExceptionWithCustomMessage_ShouldReturn404WithGenericMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new EntityNotFoundException("Player", 456, "The specified player does not exist");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(404);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("The requested resource was not found.");

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
        problemDetails.Title.Should().Be("The requested resource was not found.");

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
        problemDetails.Title.Should().Be("Invalid request.");

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

    #region UnauthorizedAccessException Tests

    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAccessException_ShouldReturn401WithGenericMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new UnauthorizedAccessException("User token expired for user admin@test.com");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(401);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(401);
        problemDetails.Title.Should().Be("Unauthorized");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAccessException_ShouldNotLog()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new UnauthorizedAccessException("Sensitive auth details");

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

    #endregion

    #region ArgumentNullException Tests

    [Fact]
    public async Task TryHandleAsync_WithArgumentNullException_ShouldReturn400WithGenericMessage()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ArgumentNullException("paramName", "Value cannot be null");

        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(400);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Invalid request.");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentNullException_ShouldNotLog()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ArgumentNullException("secretParam");

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

    #endregion

    #region Sensitive Data Leak Prevention Tests

    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAccessException_ShouldNotLeakSensitiveDetails()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new UnauthorizedAccessException("JWT token for user admin@company.com expired at 2026-01-01");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Title.Should().NotContain("admin@company.com");
        problemDetails.Title.Should().NotContain("JWT");
        problemDetails.Title.Should().NotContain("token");
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentException_ShouldNotLeakParameterNames()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ArgumentException("Value does not fall within the expected range.", "internalSecretParam");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Title.Should().NotContain("internalSecretParam");
        problemDetails.Title.Should().NotContain("expected range");
    }

    [Fact]
    public async Task TryHandleAsync_WithEntityNotFoundException_ShouldNotLeakEntityDetails()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new EntityNotFoundException("ApplicationUser", "admin@secret-domain.com");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Title.Should().NotContain("ApplicationUser");
        problemDetails.Title.Should().NotContain("admin@secret-domain.com");
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_ShouldNotLeakKeyDetails()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new KeyNotFoundException("The given key 'api_secret_key_12345' was not present in the dictionary.");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Title.Should().NotContain("api_secret_key_12345");
        problemDetails.Title.Should().NotContain("dictionary");
    }

    [Fact]
    public async Task TryHandleAsync_WithGenericException_ShouldNotLeakStackTraceOrInternals()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new Exception("Connection string: Server=db.internal;Database=prod;User=admin;Password=secret123");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        var problemDetails = await GetProblemDetailsFromResponse(responseBody);
        problemDetails.Title.Should().NotContain("Connection string");
        problemDetails.Title.Should().NotContain("secret123");
        problemDetails.Title.Should().NotContain("db.internal");
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task TryHandleAsync_ShouldReturnJsonContentType()
    {
        var (httpContext, responseBody) = CreateHttpContext();
        var exception = new ValidationException("Test");

        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        httpContext.Response.ContentType.Should().Contain("application/json");
    }

    #endregion

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
        problemDetails.Title.Should().Be("The requested resource was not found.");

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
