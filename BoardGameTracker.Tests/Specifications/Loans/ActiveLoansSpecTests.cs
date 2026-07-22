using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Loans.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Loans;

public class ActiveLoansSpecTests
{
    [Fact]
    public void Evaluate_ShouldReturnOnlyLoansWithoutReturnDate()
    {
        // Arrange
        var active = new Loan(1, 1, DateTime.UtcNow.AddDays(-3));
        var returned = new Loan(2, 1, DateTime.UtcNow.AddDays(-5));
        returned.MarkAsReturned(DateTime.UtcNow.AddDays(-1));
        var loans = new List<Loan> { active, returned };

        // Act
        var result = new ActiveLoansSpec().Evaluate(loans).ToList();

        // Assert
        result.Should().ContainSingle().Which.ReturnedDate.Should().BeNull();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeFalse_ForReturnedLoan()
    {
        // Arrange
        var returned = new Loan(1, 1, DateTime.UtcNow.AddDays(-5));
        returned.MarkAsReturned(DateTime.UtcNow.AddDays(-1));

        // Act & Assert
        new ActiveLoansSpec().IsSatisfiedBy(returned).Should().BeFalse();
    }
}
