using System;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Exceptions;

public class DomainExceptionTests
{
    #region Constructor with Message Only Tests

    [Fact]
    public void Constructor_WithMessageOnly_ShouldSetMessage()
    {
        // Arrange
        var message = "Something went wrong";

        // Act
        var exception = new DomainException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldSetDefaultErrorCode()
    {
        // Arrange
        var message = "Something went wrong";

        // Act
        var exception = new DomainException(message);

        // Assert
        exception.ErrorCode.Should().Be("DOMAIN_ERROR");
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldHaveNullInnerException()
    {
        // Arrange
        var message = "Something went wrong";

        // Act
        var exception = new DomainException(message);

        // Assert
        exception.InnerException.Should().BeNull();
    }

    #endregion

    #region Constructor with ErrorCode and Message Tests

    [Fact]
    public void Constructor_WithErrorCodeAndMessage_ShouldSetMessage()
    {
        // Arrange
        var errorCode = "INVALID_OPERATION";
        var message = "Cannot perform this operation";

        // Act
        var exception = new DomainException(errorCode, message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithErrorCodeAndMessage_ShouldSetErrorCode()
    {
        // Arrange
        var errorCode = "INVALID_OPERATION";
        var message = "Cannot perform this operation";

        // Act
        var exception = new DomainException(errorCode, message);

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void Constructor_WithErrorCodeAndMessage_ShouldHaveNullInnerException()
    {
        // Arrange
        var errorCode = "INVALID_OPERATION";
        var message = "Cannot perform this operation";

        // Act
        var exception = new DomainException(errorCode, message);

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Theory]
    [InlineData("PLAYER_NOT_FOUND", "Player was not found")]
    [InlineData("GAME_INVALID", "Game data is invalid")]
    [InlineData("SESSION_CONFLICT", "Session conflicts with existing")]
    public void Constructor_WithErrorCodeAndMessage_ShouldHandleVariousErrorCodes(string errorCode, string message)
    {
        // Act
        var exception = new DomainException(errorCode, message);

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
        exception.Message.Should().Be(message);
    }

    #endregion

    #region Constructor with Message and InnerException Tests

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetMessage()
    {
        // Arrange
        var message = "An error occurred";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DomainException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetDefaultErrorCode()
    {
        // Arrange
        var message = "An error occurred";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DomainException(message, innerException);

        // Assert
        exception.ErrorCode.Should().Be("DOMAIN_ERROR");
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetInnerException()
    {
        // Arrange
        var message = "An error occurred";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DomainException(message, innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldPreserveInnerExceptionMessage()
    {
        // Arrange
        var message = "An error occurred";
        var innerMessage = "Inner exception message";
        var innerException = new Exception(innerMessage);

        // Act
        var exception = new DomainException(message, innerException);

        // Assert
        exception.InnerException!.Message.Should().Be(innerMessage);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void DomainException_ShouldInheritFromException()
    {
        // Act
        var exception = new DomainException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void DomainException_ShouldBeThrowable()
    {
        // Arrange
        var message = "Test exception";

        // Act & Assert
        Action act = () => throw new DomainException(message);

        act.Should().Throw<DomainException>()
            .WithMessage(message);
    }

    [Fact]
    public void DomainException_ShouldBeCatchableAsException()
    {
        // Arrange
        var message = "Test exception";
        Exception? caughtException = null;

        // Act
        try
        {
            throw new DomainException(message);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<DomainException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAcceptEmptyString()
    {
        // Act
        var exception = new DomainException(string.Empty);

        // Assert
        exception.Message.Should().BeEmpty();
        exception.ErrorCode.Should().Be("DOMAIN_ERROR");
    }

    [Fact]
    public void Constructor_WithEmptyErrorCode_ShouldAcceptEmptyString()
    {
        // Act
        var exception = new DomainException(string.Empty, "Message");

        // Assert
        exception.ErrorCode.Should().BeEmpty();
    }

    #endregion
}
