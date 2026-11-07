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
        var message = "Validation failed";

        var exception = new ValidationException(message);

        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldCreateErrorsWithGeneralKey()
    {
        var message = "Validation failed";

        var exception = new ValidationException(message);

        exception.Errors.Should().ContainKey("General");
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldPutMessageInGeneralErrors()
    {
        var message = "Validation failed";

        var exception = new ValidationException(message);

        exception.Errors["General"].Should().ContainSingle()
            .Which.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageOnly_ShouldHaveSingleErrorEntry()
    {
        var message = "Validation failed";

        var exception = new ValidationException(message);

        exception.Errors.Should().HaveCount(1);
    }

    #endregion

    #region Constructor with Dictionary Tests

    [Fact]
    public void Constructor_WithDictionary_ShouldSetDefaultMessage()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Name", ["Name is required"]}
        };

        var exception = new ValidationException(errors);

        exception.Message.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldSetErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Name", ["Name is required"]},
            { "Email", ["Email is invalid"]}
        };

        var exception = new ValidationException(errors);

        exception.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldPreserveMultipleErrorsPerField()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Password", ["Password is too short", "Password must contain a number"]}
        };

        var exception = new ValidationException(errors);

        exception.Errors["Password"].Should().HaveCount(2);
        exception.Errors["Password"].Should().Contain("Password is too short");
        exception.Errors["Password"].Should().Contain("Password must contain a number");
    }

    [Fact]
    public void Constructor_WithEmptyDictionary_ShouldSetEmptyErrors()
    {
        var errors = new Dictionary<string, string[]>();

        var exception = new ValidationException(errors);

        exception.Errors.Should().BeEmpty();
    }

    #endregion

    #region Constructor with Field and Error Tests

    [Fact]
    public void Constructor_WithFieldAndError_ShouldSetErrorAsMessage()
    {
        var field = "Name";
        var error = "Name is required";

        var exception = new ValidationException(field, error);

        exception.Message.Should().Be(error);
    }

    [Fact]
    public void Constructor_WithFieldAndError_ShouldCreateErrorsWithFieldKey()
    {
        var field = "Name";
        var error = "Name is required";

        var exception = new ValidationException(field, error);

        exception.Errors.Should().ContainKey(field);
    }

    [Fact]
    public void Constructor_WithFieldAndError_ShouldPutErrorUnderFieldKey()
    {
        var field = "Name";
        var error = "Name is required";

        var exception = new ValidationException(field, error);

        exception.Errors[field].Should().ContainSingle()
            .Which.Should().Be(error);
    }

    [Fact]
    public void Constructor_WithFieldAndError_ShouldHaveSingleErrorEntry()
    {
        var field = "Name";
        var error = "Name is required";

        var exception = new ValidationException(field, error);

        exception.Errors.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAcceptEmptyString()
    {
        var exception = new ValidationException(string.Empty);

        exception.Message.Should().BeEmpty();
        exception.Errors["General"].Should().ContainSingle()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyFieldName_ShouldAcceptEmptyString()
    {
        var exception = new ValidationException(string.Empty, "Error message");

        exception.Errors.Should().ContainKey(string.Empty);
        exception.Errors[string.Empty].Should().Contain("Error message");
    }

    [Fact]
    public void Constructor_WithEmptyErrorMessage_ShouldAcceptEmptyString()
    {
        var exception = new ValidationException("Field", string.Empty);

        exception.Errors["Field"].Should().ContainSingle()
            .Which.Should().BeEmpty();
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void Constructor_WithMultipleFieldsAndMultipleErrors_ShouldPreserveAll()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Name", ["Name is required", "Name is too short"]},
            { "Email", ["Email is invalid", "Email is already taken", "Email domain not allowed"]},
            { "Password", ["Password too weak"]}
        };

        var exception = new ValidationException(errors);

        exception.Errors.Should().HaveCount(3);
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Email"].Should().HaveCount(3);
        exception.Errors["Password"].Should().HaveCount(1);
    }

    #endregion
}
