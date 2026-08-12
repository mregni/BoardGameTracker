using System;
using BoardGameTracker.Common.Entities;
using Xunit;

namespace BoardGameTracker.Tests.Core;

public class GameLoanTests
{
    [Fact]
    public void LoanToPlayer_WithNoExistingLoans_ShouldSucceed()
    {
        // Arrange
        var game = new Game("Test Game");
        var playerId = 1;
        var loanDate = DateTime.UtcNow;

        // Act
        var loan = game.LoanToPlayer(playerId, loanDate);

        // Assert
        Assert.NotNull(loan);
        Assert.Equal(game.Id, loan.GameId);
        Assert.Equal(playerId, loan.PlayerId);
        Assert.Equal(loanDate, loan.LoanDate);
        Assert.Single(game.Loans);
    }

    [Fact]
    public void IsCurrentlyLoaned_WithActiveLoan_ShouldReturnTrue()
    {
        // Arrange
        var game = new Game("Test Game");
        game.LoanToPlayer(1, DateTime.UtcNow.AddDays(-5));

        // Act
        var result = game.IsCurrentlyLoaned();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCurrentlyLoaned_WithNoLoans_ShouldReturnFalse()
    {
        // Arrange
        var game = new Game("Test Game");

        // Act
        var result = game.IsCurrentlyLoaned();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCurrentlyLoaned_WithReturnedLoan_ShouldReturnFalse()
    {
        // Arrange
        var game = new Game("Test Game");
        var loan = game.LoanToPlayer(1, DateTime.UtcNow.AddDays(-10));
        loan.MarkAsReturned(DateTime.UtcNow.AddDays(-5));

        // Act
        var result = game.IsCurrentlyLoaned();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCurrentlyLoaned_WithFutureLoan_ShouldReturnFalse()
    {
        // Arrange
        var game = new Game("Test Game");
        game.LoanToPlayer(1, DateTime.UtcNow.AddDays(5));

        // Act
        var result = game.IsCurrentlyLoaned();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoanToPlayer_ShouldOnlyLeaveTheNewestLoanActive_WhenEarlierLoansWereReturned()
    {
        var game = new Game("Test Game");

        var loan1 = game.LoanToPlayer(1, DateTime.UtcNow.AddDays(-30));
        loan1.MarkAsReturned(DateTime.UtcNow.AddDays(-25));

        var loan2 = game.LoanToPlayer(2, DateTime.UtcNow.AddDays(-20));
        loan2.MarkAsReturned(DateTime.UtcNow.AddDays(-15));

        var loan3 = game.LoanToPlayer(3, DateTime.UtcNow);

        Assert.Equal(3, game.Loans.Count);
        Assert.False(loan1.IsCurrentlyOnLoan());
        Assert.False(loan2.IsCurrentlyOnLoan());
        Assert.True(loan3.IsCurrentlyOnLoan());
    }
}
