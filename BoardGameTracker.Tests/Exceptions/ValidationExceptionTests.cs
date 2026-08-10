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
            { "Name", ["Name is required"]}
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
            { "Name", ["Name is required"]},
            { "Email", ["Email is invalid"]}
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
            { "Password", ["Password is too short", "Password must contain a number"]}
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

    #region Complex Scenarios Tests

    [Fact]
    public void Constructor_WithMultipleFieldsAndMultipleErrors_ShouldPreserveAll()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", ["Name is required", "Name is too short"]},
            { "Email", ["Email is invalid", "Email is already taken", "Email domain not allowed"]},
            { "Password", ["Password too weak"]}
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().HaveCount(3);
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Email"].Should().HaveCount(3);
        exception.Errors["Password"].Should().HaveCount(1);
    }

    #endregion
}
