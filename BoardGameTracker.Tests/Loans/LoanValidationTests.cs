using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Loans;

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
        act.Should().Throw<DomainException>()
            .WithMessage(Constants.Errors.LoanReturnedBeforeStart);
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
        act.Should().Throw<DomainException>()
            .WithMessage(Constants.Errors.LoanAlreadyReturned);
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
        act.Should().Throw<DomainException>()
            .WithMessage(Constants.Errors.LoanReturnedBeforeStart);
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
        act.Should().Throw<DomainException>()
            .WithMessage(Constants.Errors.LoanDueBeforeStart);
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
        act.Should().Throw<DomainException>()
            .WithMessage(Constants.Errors.LoanDueBeforeStart);
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
        var result = loan.IsCurrentlyOnLoan(DateTime.UtcNow);

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
        var result = loan.IsCurrentlyOnLoan(DateTime.UtcNow);

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
        var result = loan.IsCurrentlyOnLoan(DateTime.UtcNow);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyOnLoan_WithFutureReturnDate_ShouldReturnFalse()
    {
        // Arrange
        var loanDate = DateTime.UtcNow.AddDays(-5);
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        loan.MarkAsReturned(DateTime.UtcNow.AddDays(2));

        // Act
        var result = loan.IsCurrentlyOnLoan(DateTime.UtcNow);

        // Assert
        result.Should().BeFalse();
    }

    private static readonly DateTime Anchor = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(-1, 1)]
    public void Constructor_ShouldRejectInvalidIds(int gameId, int playerId)
    {
        var act = () => new Loan(gameId, playerId, Anchor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldAllowUnsavedGame_WithZeroId()
    {
        var loan = new Loan(0, 1, Anchor);

        loan.GameId.Should().Be(0);
    }

    [Fact]
    public void IsCurrentlyOnLoan_WhenTheLoanStartsThisVeryMoment_ShouldReturnTrue()
    {
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        new Loan(1, 1, now).IsCurrentlyOnLoan(now).Should().BeTrue();
    }

    [Fact]
    public void UndoReturn_ShouldClearTheReturnedDate()
    {
        var loan = new Loan(1, 1, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        loan.MarkAsReturned(new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        loan.UndoReturn();

        loan.ReturnedDate.Should().BeNull();
    }

    [Theory]
    [InlineData(10, 20, null, null, true)]
    [InlineData(10, 20, 11, null, true)]
    [InlineData(5, 10, 11, null, false)]
    [InlineData(5, 11, 11, null, false)]
    [InlineData(5, 12, 11, null, true)]
    [InlineData(5, null, 11, 15, true)]
    [InlineData(14, null, 11, 15, true)]
    [InlineData(15, null, 11, 15, false)]
    [InlineData(16, null, 11, 15, false)]
    public void Overlaps_ShouldTreatTheEndOfARangeAsExclusive(int startDay, int? endDay, int? loanDay, int? returnDay, bool expected)
    {
        var september = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var loan = new Loan(1, 1, september.AddDays((loanDay ?? 1) - 1));
        if (returnDay.HasValue)
        {
            loan.MarkAsReturned(september.AddDays(returnDay.Value - 1));
        }

        var end = endDay.HasValue ? september.AddDays(endDay.Value - 1) : (DateTime?)null;

        loan.Overlaps(september.AddDays(startDay - 1), end).Should().Be(expected);
    }
}
