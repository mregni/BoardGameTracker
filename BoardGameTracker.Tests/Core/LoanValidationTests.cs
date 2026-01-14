using BoardGameTracker.Common.Entities;
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
        Assert.Equal(loanDate, loan.LoanDate);
        Assert.Equal(1, loan.GameId);
        Assert.Equal(1, loan.PlayerId);
    }

    [Fact]
    public void Constructor_WithLoanDateTooFarInPast_ShouldThrow()
    {
        // Arrange
        var elevenYearsAgo = DateTime.UtcNow.AddYears(-11);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Loan(gameId: 1, playerId: 1, elevenYearsAgo));

        Assert.Contains("10 years in the past", ex.Message);
    }

    [Fact]
    public void Constructor_WithLoanDateTooFarInFuture_ShouldThrow()
    {
        // Arrange
        var twoYearsFromNow = DateTime.UtcNow.AddYears(2);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Loan(gameId: 1, playerId: 1, twoYearsFromNow));

        Assert.Contains("1 year in the future", ex.Message);
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
        Assert.Equal(returnDate, loan.ReturnedDate);
    }

    [Fact]
    public void MarkAsReturned_WithReturnBeforeLoan_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var returnDate = loanDate.AddDays(-1); // Before loan date
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.MarkAsReturned(returnDate));

        Assert.Contains("Return date cannot be before loan date", ex.Message);
    }

    [Fact]
    public void MarkAsReturned_WhenAlreadyReturned_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow.AddDays(-5);
        var returnDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        loan.MarkAsReturned(returnDate);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            loan.MarkAsReturned(DateTime.UtcNow));

        Assert.Contains("already been returned", ex.Message);
    }

    [Fact]
    public void MarkAsReturned_WithReturnDateTooFarInFuture_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var threeYearsFromNow = loanDate.AddYears(3);
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.MarkAsReturned(threeYearsFromNow));

        Assert.Contains("2 years after loan date", ex.Message);
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
        Assert.Equal(newLoanDate, loan.LoanDate);
        Assert.Equal(dueDate, loan.DueDate);
        Assert.Equal(returnDate, loan.ReturnedDate);
    }

    [Fact]
    public void UpdateDates_WithReturnBeforeLoan_ShouldThrow()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow.AddDays(-5));
        var loanDate = DateTime.UtcNow;
        var returnDate = loanDate.AddDays(-1); // Before loan

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.UpdateDates(loanDate, null, returnDate));

        Assert.Contains("Return date cannot be before loan date", ex.Message);
    }

    [Fact]
    public void UpdateDates_WithDueBeforeLoan_ShouldThrow()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow.AddDays(-5));
        var loanDate = DateTime.UtcNow;
        var dueDate = loanDate.AddDays(-1); // Before loan

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.UpdateDates(loanDate, dueDate, null));

        Assert.Contains("Due date cannot be before loan date", ex.Message);
    }

    [Fact]
    public void UpdateDates_WithLoanDateTooFarInPast_ShouldThrow()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow.AddDays(-5));
        var elevenYearsAgo = DateTime.UtcNow.AddYears(-11);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.UpdateDates(elevenYearsAgo, null, null));

        Assert.Contains("10 years in the past", ex.Message);
    }

    [Fact]
    public void UpdateDates_WithLoanDateTooFarInFuture_ShouldThrow()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow.AddDays(-5));
        var twoYearsFromNow = DateTime.UtcNow.AddYears(2);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.UpdateDates(twoYearsFromNow, null, null));

        Assert.Contains("1 year in the future", ex.Message);
    }

    [Fact]
    public void SetDueDate_WithDueBeforeLoan_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        var dueDate = loanDate.AddDays(-1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.SetDueDate(dueDate));

        Assert.Contains("Due date cannot be before loan date", ex.Message);
    }

    [Fact]
    public void SetDueDate_WithDueTooFarInFuture_ShouldThrow()
    {
        // Arrange
        var loanDate = DateTime.UtcNow;
        var loan = new Loan(gameId: 1, playerId: 1, loanDate);
        var threeYearsFromNow = loanDate.AddYears(3);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            loan.SetDueDate(threeYearsFromNow));

        Assert.Contains("2 years after loan date", ex.Message);
    }

    [Fact]
    public void SetDueDate_WithNull_ShouldSucceed()
    {
        // Arrange
        var loan = new Loan(gameId: 1, playerId: 1, DateTime.UtcNow);

        // Act
        loan.SetDueDate(null);

        // Assert
        Assert.Null(loan.DueDate);
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
        Assert.True(result);
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
        Assert.False(result);
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
        Assert.False(result);
    }
}
