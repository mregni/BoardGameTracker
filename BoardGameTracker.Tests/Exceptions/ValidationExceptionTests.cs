using System;
using System.Collections.Generic;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Exceptions;

public class ValidationExceptionTests
{
    #region Constructor with Message Only Tests

    [Fact]
    public void Constructor_WithMessageOnly_ShouldSetMessage()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldCreateErrorsWithGeneralKey()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Errors.Should().ContainKey("General");
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldPutMessageInGeneralErrors()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Errors["General"].Should().ContainSingle()
            .Which.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldHaveSingleErrorEntry()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Errors.Should().HaveCount(1);
    }

    #endregion

    #region Constructor with Dictionary Tests

    [Fact]
    public void Constructor_WithDictionary_ShouldSetDefaultMessage()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required" } }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldSetErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required" } },
            { "Email", new[] { "Email is invalid" } }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldPreserveMultipleErrorsPerField()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Password", new[] { "Password is too short", "Password must contain a number" } }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors["Password"].Should().HaveCount(2);
        exception.Errors["Password"].Should().Contain("Password is too short");
        exception.Errors["Password"].Should().Contain("Password must contain a number");
    }

    [Fact]
    public void Constructor_WithEmptyDictionary_ShouldSetEmptyErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>();

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().BeEmpty();
    }

    #endregion

    #region Constructor with Field and Error Tests

    [Fact]
    public void Constructor_WithFieldAndError_ShouldSetErrorAsMessage()
    {
        // Arrange
        var field = "Name";
        var error = "Name is required";

        // Act
        var exception = new ValidationException(field, error);

        // Assert
        exception.Message.Should().Be(error);
    }

    [Fact]
    public void Constructor_WithFieldAndError_ShouldCreateErrorsWithFieldKey()
    {
        // Arrange
        var field = "Name";
        var error = "Name is required";

        // Act
        var exception = new ValidationException(field, error);

        // Assert
        exception.Errors.Should().ContainKey(field);
    }

    [Fact]
    public void Constructor_WithFieldAndError_ShouldPutErrorUnderFieldKey()
    {
        // Arrange
        var field = "Name";
        var error = "Name is required";

        // Act
        var exception = new ValidationException(field, error);

        // Assert
        exception.Errors[field].Should().ContainSingle()
            .Which.Should().Be(error);
    }

    [Fact]
    public void Constructor_WithFieldAndError_ShouldHaveSingleErrorEntry()
    {
        // Arrange
        var field = "Name";
        var error = "Name is required";

        // Act
        var exception = new ValidationException(field, error);

        // Assert
        exception.Errors.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("Email", "Email is invalid")]
    [InlineData("Age", "Age must be positive")]
    [InlineData("Password", "Password is too weak")]
    public void Constructor_WithFieldAndError_ShouldHandleVariousFields(string field, string error)
    {
        // Act
        var exception = new ValidationException(field, error);

        // Assert
        exception.Errors.Should().ContainKey(field);
        exception.Errors[field].Should().Contain(error);
        exception.Message.Should().Be(error);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void ValidationException_ShouldInheritFromException()
    {
        // Act
        var exception = new ValidationException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ValidationException_ShouldBeThrowable()
    {
        // Arrange
        var message = "Validation failed";

        // Act & Assert
        Action act = () => throw new ValidationException(message);

        act.Should().Throw<ValidationException>()
            .WithMessage(message);
    }

    [Fact]
    public void ValidationException_ShouldBeCatchableAsException()
    {
        // Arrange
        Exception? caughtException = null;

        // Act
        try
        {
            throw new ValidationException("Test");
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<ValidationException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAcceptEmptyString()
    {
        // Act
        var exception = new ValidationException(string.Empty);

        // Assert
        exception.Message.Should().BeEmpty();
        exception.Errors["General"].Should().ContainSingle()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyFieldName_ShouldAcceptEmptyString()
    {
        // Act
        var exception = new ValidationException(string.Empty, "Error message");

        // Assert
        exception.Errors.Should().ContainKey(string.Empty);
        exception.Errors[string.Empty].Should().Contain("Error message");
    }

    [Fact]
    public void Constructor_WithEmptyErrorMessage_ShouldAcceptEmptyString()
    {
        // Act
        var exception = new ValidationException("Field", string.Empty);

        // Assert
        exception.Errors["Field"].Should().ContainSingle()
            .Which.Should().BeEmpty();
    }

    #endregion

    #region InnerException Tests

    [Fact]
    public void ValidationException_ShouldHaveNullInnerException()
    {
        // Act
        var exception = new ValidationException("Test");

        // Assert
        exception.InnerException.Should().BeNull();
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void Constructor_WithMultipleFieldsAndMultipleErrors_ShouldPreserveAll()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required", "Name is too short" } },
            { "Email", new[] { "Email is invalid", "Email is already taken", "Email domain not allowed" } },
            { "Password", new[] { "Password too weak" } }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().HaveCount(3);
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Email"].Should().HaveCount(3);
        exception.Errors["Password"].Should().HaveCount(1);
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldBeModifiable()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required" } }
        };
        var exception = new ValidationException(errors);

        // Act - The errors dictionary is the same reference
        exception.Errors["Email"] = new[] { "Email is invalid" };

        // Assert
        exception.Errors.Should().HaveCount(2);
    }

    #endregion
}
