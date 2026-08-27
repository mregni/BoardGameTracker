using System;
using BoardGameTracker.Common.Entities;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Core;

public class LoanValidationTests
{
    [Fact]
    public void Constructor_WithValidDates_ShouldSucceed()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;

        // Act
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Assert
        loan.LoanDate.Should().Be(loanDate);
        loan.GameId.Should().Be(1);
        loan.PlayerId.Should().Be(1);
        loan.DueDate.Should().BeNull();
        loan.ReturnedDate.Should().BeNull();
    }

    [Fact]
    public void MarkAsReturned_WithValidReturnDate_ShouldSucceed()
    {
        // Arrange
        var loanDate = DateTime.UtcNow.AddDays(-5);
        var returnDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act
        loan.MarkAsReturned(returnDate);

        // Assert
        loan.ReturnedDate.Should().Be(returnDate);
    }

    [Fact]
    public void MarkAsReturned_WithReturnDateEqualToLoanDate_ShouldSucceed()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act
        loan.MarkAsReturned(loanDate);

        // Assert
        loan.ReturnedDate.Should().Be(loanDate);
    }

    [Fact]
    public void MarkAsReturned_WithReturnBeforeLoan_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var returnDate = loanDate.AddDays(-1);
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act
        var act = () => loan.MarkAsReturned(returnDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Return date cannot be before loan date*");
        loan.ReturnedDate.Should().BeNull();
    }

    [Fact]
    public void MarkAsReturned_WhenAlreadyReturned_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow.AddDays(-5);
        var returnDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        loan.MarkAsReturned(returnDate);

        // Act
        var act = () => loan.MarkAsReturned(DateTime.UtcNow);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been returned*");
        loan.ReturnedDate.Should().Be(returnDate);
    }

    [Fact]
    public void UpdateDates_WithValidDates_ShouldSucceed()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow.AddDays(-5));
        var newLoanDate = DateTime.UtcNow.AddDays(-3);
        var dueDate = DateTime.UtcNow.AddDays(7);
        var returnDate = DateTime.UtcNow;

        // Act
        loan.UpdateDates(newLoanDate, dueDate, returnDate);

        // Assert
        loan.LoanDate.Should().Be(newLoanDate);
        loan.DueDate.Should().Be(dueDate);
        loan.ReturnedDate.Should().Be(returnDate);
    }

    [Fact]
    public void UpdateDates_WithNullDueAndReturnDates_ShouldClearThem()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow.AddDays(-10));
        loan.SetDueDate(DateTime.UtcNow.AddDays(-2));
        loan.MarkAsReturned(DateTime.UtcNow.AddDays(-5));
        var newLoanDate = DateTime.UtcNow.AddDays(-3);

        // Act
        loan.UpdateDates(newLoanDate, null, null);

        // Assert
        loan.LoanDate.Should().Be(newLoanDate);
        loan.DueDate.Should().BeNull();
        loan.ReturnedDate.Should().BeNull();
    }

    [Fact]
    public void UpdateDates_WithReturnBeforeLoan_ShouldThrow()
    {
        // Arrange
        var originalLoanDate = DateTime.UtcNow.AddDays(-5);
        var loan = new Loan(gameId: 1, playerId: 1, originalLoanDate);
        var loanDate = DateTime.UtcNow;
        var returnDate = loanDate.AddDays(-1);

        // Act
        var act = () => loan.UpdateDates(loanDate, null, returnDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Return date cannot be before loan date*");
        loan.LoanDate.Should().Be(originalLoanDate);
        loan.DueDate.Should().BeNull();
        loan.ReturnedDate.Should().BeNull();
    }

    [Fact]
    public void UpdateDates_WithDueBeforeLoan_ShouldThrow()
    {
        // Arrange
        var originalLoanDate = DateTime.UtcNow.AddDays(-5);
        var loan = new Loan(gameId: 1, playerId: 1, originalLoanDate);
        var loanDate = DateTime.UtcNow;
        var dueDate = loanDate.AddDays(-1);

        // Act
        var act = () => loan.UpdateDates(loanDate, dueDate, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Due date cannot be before loan date*");
        loan.LoanDate.Should().Be(originalLoanDate);
        loan.DueDate.Should().BeNull();
        loan.ReturnedDate.Should().BeNull();
    }

    [Fact]
    public void SetDueDate_WithValidDueDate_ShouldSucceed()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var dueDate = loanDate.AddDays(14);
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act
        loan.SetDueDate(dueDate);

        // Assert
        loan.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void SetDueDate_WithDueDateEqualToLoanDate_ShouldSucceed()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act
        loan.SetDueDate(loanDate);

        // Assert
        loan.DueDate.Should().Be(loanDate);
    }

    [Fact]
    public void SetDueDate_WithDueBeforeLoan_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        var dueDate = loanDate.AddDays(-1);

        // Act
        var act = () => loan.SetDueDate(dueDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Due date cannot be before loan date*");
        loan.DueDate.Should().BeNull();
    }

    [Fact]
    public void SetDueDate_WithNull_ShouldSucceed()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow);

        // Act
        loan.SetDueDate(null);

        // Assert
        loan.DueDate.Should().BeNull();
    }

    [Fact]
    public void IsCurrentlyOnLoan_WhenNotReturned_ShouldReturnTrue()
    {
        // Arrange
        var loanDate = DateTime.UtcNow.AddDays(-5);
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act
        var result = loan.IsCurrentlyOnLoan();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyOnLoan_WhenReturned_ShouldReturnFalse()
    {
        // Arrange
        var loanDate = DateTime.UtcNow.AddDays(-5);
        var returnDate = DateTime.UtcNow.AddDays(-1);
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        loan.MarkAsReturned(returnDate);

        // Act
        var result = loan.IsCurrentlyOnLoan();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyOnLoan_WithFutureLoanDate_ShouldReturnFalse()
    {
        // Arrange
        var futureLoanDate = DateTime.UtcNow.AddDays(5);
        var loan = new Loan(gameId: 1, playerId: 1, futureLoanDate);

        // Act
        var result = loan.IsCurrentlyOnLoan();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyOnLoan_WithFutureReturnDate_PinsCurrentBehavior()
    {
        // Arrange
        var loanDate = DateTime.UtcNow.AddDays(-5);
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        loan.MarkAsReturned(DateTime.UtcNow.AddDays(2));

        // Act
        var result = loan.IsCurrentlyOnLoan();

        // Assert
        result.Should().BeTrue();
    }

    private static readonly DateTime Anchor = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsActiveOn_BeforeLoanDate_ShouldReturnFalse()
    {
        var loan = new Loan(1, 1, Anchor);

        loan.IsActiveOn(Anchor.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void IsActiveOn_OpenLoanWithoutDueDate_ShouldReturnTrue_ForAnyDateFromLoanDate()
    {
        var loan = new Loan(1, 1, Anchor);

        loan.IsActiveOn(Anchor).Should().BeTrue();
        loan.IsActiveOn(Anchor.AddYears(1)).Should().BeTrue();
    }

    [Fact]
    public void IsActiveOn_WithDueDate_ShouldUseDueDateAsExpectedEnd()
    {
        var loan = new Loan(1, 1, Anchor);
        loan.SetDueDate(Anchor.AddDays(7));

        loan.IsActiveOn(Anchor.AddDays(3)).Should().BeTrue();
        loan.IsActiveOn(Anchor.AddDays(7)).Should().BeFalse();
        loan.IsActiveOn(Anchor.AddDays(10)).Should().BeFalse();
    }

    [Fact]
    public void IsActiveOn_ReturnedDate_ShouldTakePriorityOverDueDate()
    {
        var loan = new Loan(1, 1, Anchor);
        loan.SetDueDate(Anchor.AddDays(7));
        loan.MarkAsReturned(Anchor.AddDays(2));

        loan.IsActiveOn(Anchor.AddDays(1)).Should().BeTrue();
        loan.IsActiveOn(Anchor.AddDays(2)).Should().BeFalse();
        loan.IsActiveOn(Anchor.AddDays(5)).Should().BeFalse();
    }
}
