using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Bgg;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class StatusExtensionsTests
{
    [Fact]
    public void ToGameState_ShouldReturnPreviouslyOwned_WhenPrevownedIs1()
    {
        // Arrange
        var status = new Status
        {
            Prevowned = 1,
            Fortrade = 0,
            Want = 0,
            Own = 0,
            LastModified = "2023-01-01 00:00:00"
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.PreviouslyOwned);
    }

    [Fact]
    public void ToGameState_ShouldReturnForTrade_WhenFortradeIs1()
    {
        // Arrange
        var status = new Status
        {
            Prevowned = 0,
            Fortrade = 1,
            Want = 0,
            Own = 0,
            LastModified = "2023-01-01 00:00:00"
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.ForTrade);
    }

    [Fact]
    public void ToGameState_ShouldReturnWanted_WhenWantIs1()
    {
        // Arrange
        var status = new Status
        {
            Prevowned = 0,
            Fortrade = 0,
            Want = 1,
            Own = 0,
            LastModified = "2023-01-01 00:00:00"
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
        var status = new Status
        {
            Prevowned = 0,
            Fortrade = 0,
            Want = 0,
            Own = 1,
            LastModified = "2023-01-01 00:00:00"
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.Owned);
    }

    [Fact]
    public void ToGameState_ShouldReturnOwned_WhenAllStatusAreZero()
    {
        // Arrange
        var status = new Status
        {
            Prevowned = 0,
            Fortrade = 0,
            Want = 0,
            Own = 0,
            LastModified = "2023-01-01 00:00:00"
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
        var status = new Status
        {
            Prevowned = 1,
            Fortrade = 1,
            Want = 1,
            Own = 1,
            LastModified = "2023-01-01 00:00:00"
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
        var status = new Status
        {
            Prevowned = 0,
            Fortrade = 1,
            Want = 1,
            Own = 1,
            LastModified = "2023-01-01 00:00:00"
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
        var status = new Status
        {
            Prevowned = 0,
            Fortrade = 0,
            Want = 1,
            Own = 1,
            LastModified = "2023-01-01 00:00:00"
        };

        // Act
        var result = status.ToGameState();

        // Assert
        result.Should().Be(GameState.Wanted);
    }
}
