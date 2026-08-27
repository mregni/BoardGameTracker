using System;
using BoardGameTracker.Common.Entities;
using FluentAssertions;
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
        loan.Should().NotBeNull();
        loan.PlayerId.Should().Be(playerId);
        loan.LoanDate.Should().Be(loanDate);
        loan.DueDate.Should().BeNull();
        loan.ReturnedDate.Should().BeNull();
        game.Loans.Should().ContainSingle().Which.Should().BeSameAs(loan);
    }

    [Fact]
    public void LoanToPlayer_WhileAlreadyLoaned_PinsCurrentBehavior()
    {
        // Arrange
        var game = new Game("Test Game");
        var firstLoan = game.LoanToPlayer(1, DateTime.UtcNow.AddDays(-5));

        // Act
        var secondLoan = game.LoanToPlayer(2, DateTime.UtcNow);

        // Assert
        game.Loans.Should().HaveCount(2);
        firstLoan.IsCurrentlyOnLoan().Should().BeTrue();
        secondLoan.IsCurrentlyOnLoan().Should().BeTrue();
        game.IsCurrentlyLoaned().Should().BeTrue();
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
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyLoaned_WithNoLoans_ShouldReturnFalse()
    {
        // Arrange
        var game = new Game("Test Game");

        // Act
        var result = game.IsCurrentlyLoaned();

        // Assert
        result.Should().BeFalse();
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

        game.Loans.Should().HaveCount(3);
        loan1.IsCurrentlyOnLoan().Should().BeFalse();
        loan2.IsCurrentlyOnLoan().Should().BeFalse();
        loan3.IsCurrentlyOnLoan().Should().BeTrue();
    }
}
