using System;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
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
    public void LoanToPlayer_WhileAlreadyLoaned_ShouldThrow()
    {
        var game = new Game("Test Game");
        var firstLoan = game.LoanToPlayer(1, DateTime.UtcNow.AddDays(-5));

        var act = () => game.LoanToPlayer(2, DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.GameAlreadyOnLoan);
        game.Loans.Should().ContainSingle().Which.Should().BeSameAs(firstLoan);
        game.IsCurrentlyLoaned().Should().BeTrue();
    }

    [Fact]
    public void LoanToPlayer_AfterPreviousLoanReturned_ShouldSucceed()
    {
        var game = new Game("Test Game");
        var firstLoan = game.LoanToPlayer(1, DateTime.UtcNow.AddDays(-5));
        firstLoan.MarkAsReturned(DateTime.UtcNow.AddDays(-1));

        var secondLoan = game.LoanToPlayer(2, DateTime.UtcNow);

        game.Loans.Should().HaveCount(2);
        secondLoan.IsCurrentlyOnLoan().Should().BeTrue();
    }

    [Fact]
    public void LoanToPlayer_ForFutureDateAfterDueDate_ShouldSucceed()
    {
        var game = new Game("Test Game");
        var firstLoan = game.LoanToPlayer(1, DateTime.UtcNow);
        firstLoan.SetDueDate(DateTime.UtcNow.AddDays(7));

        var secondLoan = game.LoanToPlayer(2, DateTime.UtcNow.AddDays(10));

        game.Loans.Should().HaveCount(2);
        secondLoan.PlayerId.Should().Be(2);
    }

    [Fact]
    public void LoanToPlayer_ForFutureDateBeforeDueDate_ShouldThrow()
    {
        var game = new Game("Test Game");
        var firstLoan = game.LoanToPlayer(1, DateTime.UtcNow);
        firstLoan.SetDueDate(DateTime.UtcNow.AddDays(7));

        var act = () => game.LoanToPlayer(2, DateTime.UtcNow.AddDays(3));

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.GameAlreadyOnLoan);
        game.Loans.Should().ContainSingle();
    }

    [Fact]
    public void LoanToPlayer_ForFutureDate_WhileOpenLoanHasNoDueDate_ShouldThrow()
    {
        var game = new Game("Test Game");
        game.LoanToPlayer(1, DateTime.UtcNow);

        var act = () => game.LoanToPlayer(2, DateTime.UtcNow.AddDays(30));

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.GameAlreadyOnLoan);
        game.Loans.Should().ContainSingle();
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
