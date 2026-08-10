using System;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Exceptions;

public class EntityNotFoundExceptionTests
{
    #region Constructor with EntityType and EntityId Tests

    [Fact]
    public void Constructor_WithEntityTypeAndId_ShouldSetEntityType()
    {
        // Arrange
        var entityType = "Player";
        var entityId = 123;

        // Act
        var exception = new EntityNotFoundException(entityType, entityId);

        // Assert
        exception.EntityType.Should().Be(entityType);
    }

    [Fact]
    public void Constructor_WithEntityTypeAndId_ShouldSetEntityId()
    {
        // Arrange
        var entityType = "Player";
        var entityId = 123;

        // Act
        var exception = new EntityNotFoundException(entityType, entityId);

        // Assert
        exception.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void Constructor_WithEntityTypeAndId_ShouldGenerateDefaultMessage()
    {
        // Arrange
        var entityType = "Player";
        var entityId = 123;

        // Act
        var exception = new EntityNotFoundException(entityType, entityId);

        // Assert
        exception.Message.Should().Be("Player with ID '123' was not found.");
    }

    [Theory]
    [InlineData("Game", 1, "Game with ID '1' was not found.")]
    [InlineData("Session", 42, "Session with ID '42' was not found.")]
    [InlineData("Badge", 100, "Badge with ID '100' was not found.")]
    public void Constructor_WithEntityTypeAndId_ShouldGenerateCorrectMessage(
        string entityType, int entityId, string expectedMessage)
    {
        // Act
        var exception = new EntityNotFoundException(entityType, entityId);

        // Assert
        exception.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void Constructor_WithEntityTypeAndId_ShouldHandleGuidId()
    {
        // Arrange
        var entityType = "User";
        var entityId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var exception = new EntityNotFoundException(entityType, entityId);

        // Assert
        exception.EntityId.Should().Be(entityId);
        exception.Message.Should().Contain("12345678-1234-1234-1234-123456789012");
    }

    [Fact]
    public void Constructor_WithEntityTypeAndId_ShouldHandleStringId()
    {
        // Arrange
        var entityType = "Document";
        var entityId = "doc-123-abc";

        // Act
        var exception = new EntityNotFoundException(entityType, entityId);

        // Assert
        exception.EntityId.Should().Be(entityId);
        exception.Message.Should().Be("Document with ID 'doc-123-abc' was not found.");
    }

    #endregion

    #region Constructor with EntityType, EntityId, and Custom Message Tests

    [Fact]
    public void Constructor_WithCustomMessage_ShouldSetEntityType()
    {
        // Arrange
        var entityType = "Player";
        var entityId = 123;
        var message = "Custom error message";

        // Act
        var exception = new EntityNotFoundException(entityType, entityId, message);

        // Assert
        exception.EntityType.Should().Be(entityType);
    }

    [Fact]
    public void Constructor_WithCustomMessage_ShouldSetEntityId()
    {
        // Arrange
        var entityType = "Player";
        var entityId = 123;
        var message = "Custom error message";

        // Act
        var exception = new EntityNotFoundException(entityType, entityId, message);

        // Assert
        exception.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void Constructor_WithCustomMessage_ShouldOverrideDefaultMessage()
    {
        // Arrange
        var entityType = "Player";
        var entityId = 123;
        var customMessage = "Player not available";

        // Act
        var exception = new EntityNotFoundException(entityType, entityId, customMessage);

        // Assert
        exception.Message.Should().NotBe("Player with ID '123' was not found.");
        exception.Message.Should().Be(customMessage);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithZeroId_ShouldAcceptZero()
    {
        // Act
        var exception = new EntityNotFoundException("Player", 0);

        // Assert
        exception.EntityId.Should().Be(0);
        exception.Message.Should().Be("Player with ID '0' was not found.");
    }

    [Fact]
    public void Constructor_WithNegativeId_ShouldAcceptNegative()
    {
        // Act
        var exception = new EntityNotFoundException("Player", -1);

        // Assert
        exception.EntityId.Should().Be(-1);
        exception.Message.Should().Be("Player with ID '-1' was not found.");
    }

    #endregion
}
