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
