using System;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class StatusExtensionsTests
{
    [Fact]
    public void ToGameState_ShouldReturnPreviouslyOwned_WhenPreviouslyOwnedIsTrue()
    {
        // Arrange
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = true,
            ForTrade = false,
            Want = false,
            Owned = false,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.PreviouslyOwned);
    }

    [Fact]
    public void ToGameState_ShouldReturnForTrade_WhenForTradeIsTrue()
    {
        // Arrange
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = false,
            ForTrade = true,
            Want = false,
            Owned = false,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.ForTrade);
    }

    [Fact]
    public void ToGameState_ShouldReturnWanted_WhenWantIsTrue()
    {
        // Arrange
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = false,
            ForTrade = false,
            Want = true,
            Owned = false,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.Wanted);
    }

    [Fact]
    public void ToGameState_ShouldReturnOwned_WhenNoOtherStatusSet()
    {
        // Arrange
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = false,
            ForTrade = false,
            Want = false,
            Owned = true,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.Owned);
    }

    [Fact]
    public void ToGameState_ShouldReturnOwned_WhenAllStatusAreFalse()
    {
        // Arrange
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = false,
            ForTrade = false,
            Want = false,
            Owned = false,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.Owned);
    }

    [Fact]
    public void ToGameState_ShouldPrioritizePreviouslyOwned_WhenMultipleStatusSet()
    {
        // Arrange - All flags set, should prioritize PreviouslyOwned
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = true,
            ForTrade = true,
            Want = true,
            Owned = true,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.PreviouslyOwned);
    }

    [Fact]
    public void ToGameState_ShouldPrioritizeForTrade_OverWantedAndOwned()
    {
        // Arrange
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = false,
            ForTrade = true,
            Want = true,
            Owned = true,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.ForTrade);
    }

    [Fact]
    public void ToGameState_ShouldPrioritizeWanted_OverOwned()
    {
        // Arrange
        var status = new CollectionResponse.Status
        {
            PreviouslyOwned = false,
            ForTrade = false,
            Want = true,
            Owned = true,
            LastModified = new DateTime(2023, 1, 1)
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.Wanted);
    }
}
