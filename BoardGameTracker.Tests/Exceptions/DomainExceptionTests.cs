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
        var message = "Something went wrong";

        var exception = new DomainException(message);

        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldSetDefaultErrorCode()
    {
        var message = "Something went wrong";

        var exception = new DomainException(message);

        exception.ErrorCode.Should().Be("DOMAIN_ERROR");
    }

    #endregion

    #region Constructor with ErrorCode and Message Tests

    [Fact]
    public void Constructor_WithErrorCodeAndMessage_ShouldSetMessage()
    {
        var errorCode = "INVALID_OPERATION";
        var message = "Cannot perform this operation";

        var exception = new DomainException(errorCode, message);

        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithErrorCodeAndMessage_ShouldSetErrorCode()
    {
        var errorCode = "INVALID_OPERATION";
        var message = "Cannot perform this operation";

        var exception = new DomainException(errorCode, message);

        exception.ErrorCode.Should().Be(errorCode);
    }

    #endregion

    #region Constructor with Message and InnerException Tests

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetMessage()
    {
        var message = "An error occurred";
        var innerException = new InvalidOperationException("Inner error");

        var exception = new DomainException(message, innerException);

        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetDefaultErrorCode()
    {
        var message = "An error occurred";
        var innerException = new InvalidOperationException("Inner error");

        var exception = new DomainException(message, innerException);

        exception.ErrorCode.Should().Be("DOMAIN_ERROR");
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetInnerException()
    {
        var message = "An error occurred";
        var innerException = new InvalidOperationException("Inner error");

        var exception = new DomainException(message, innerException);

        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAcceptEmptyString()
    {
        var exception = new DomainException(string.Empty);

        exception.Message.Should().BeEmpty();
        exception.ErrorCode.Should().Be("DOMAIN_ERROR");
    }

    [Fact]
    public void Constructor_WithEmptyErrorCode_ShouldAcceptEmptyString()
    {
        var exception = new DomainException(string.Empty, "Message");

        exception.ErrorCode.Should().BeEmpty();
    }

    #endregion
}
