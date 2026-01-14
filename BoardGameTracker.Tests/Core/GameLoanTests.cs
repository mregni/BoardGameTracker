using System;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
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
    public void LoanToPlayer_WithFutureLoanButDueDateBeforeFuture_ShouldSucceed()
    {
        // Arrange
        var game = new Game("Test Game");
        var playerId1 = 1;
        var playerId2 = 2;
        var futureLoanDate = DateTime.UtcNow.AddDays(10);
        var todayLoanDate = DateTime.UtcNow;
        var dueDate = DateTime.UtcNow.AddDays(5); // Before future loan starts

        // Create future loan
        game.LoanToPlayer(playerId1, futureLoanDate);

        // Act - should succeed because loan will be returned before future loan starts
        var loan = game.LoanToPlayer(playerId2, todayLoanDate, dueDate);

        // Assert
        Assert.NotNull(loan);
        Assert.Equal(2, game.Loans.Count);
    }

    [Fact]
    public void LoanToPlayer_WithReturnedLoan_ShouldSucceed()
    {
        // Arrange
        var game = new Game("Test Game");
        var firstPlayerId = 1;
        var secondPlayerId = 2;
        var firstLoanDate = DateTime.UtcNow.AddDays(-10);
        var returnDate = DateTime.UtcNow.AddDays(-5);

        // Create and return first loan
        var firstLoan = game.LoanToPlayer(firstPlayerId, firstLoanDate);
        firstLoan.MarkAsReturned(returnDate);

        // Act - should succeed because first loan is returned
        var secondLoan = game.LoanToPlayer(secondPlayerId, DateTime.UtcNow);

        // Assert
        Assert.NotNull(secondLoan);
        Assert.Equal(2, game.Loans.Count);
        Assert.Equal(secondPlayerId, secondLoan.PlayerId);
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
    public void LoanToPlayer_WithMultipleReturnedLoans_ShouldSucceed()
    {
        // Arrange
        var game = new Game("Test Game");

        // Create and return first loan
        var loan1 = game.LoanToPlayer(1, DateTime.UtcNow.AddDays(-30));
        loan1.MarkAsReturned(DateTime.UtcNow.AddDays(-25));

        // Create and return second loan
        var loan2 = game.LoanToPlayer(2, DateTime.UtcNow.AddDays(-20));
        loan2.MarkAsReturned(DateTime.UtcNow.AddDays(-15));

        // Act - should succeed because all previous loans are returned
        var loan3 = game.LoanToPlayer(3, DateTime.UtcNow);

        // Assert
        Assert.NotNull(loan3);
        Assert.Equal(3, game.Loans.Count);
        Assert.False(loan1.IsCurrentlyOnLoan());
        Assert.False(loan2.IsCurrentlyOnLoan());
        Assert.True(loan3.IsCurrentlyOnLoan());
    }
}
