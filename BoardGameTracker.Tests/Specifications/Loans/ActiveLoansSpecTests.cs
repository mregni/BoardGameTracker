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
        var active = new Loan(1, 1, DateTime.UtcNow.AddDays(-3)) { Id = 1 };
        var returned = new Loan(2, 1, DateTime.UtcNow.AddDays(-5)) { Id = 2 };
        returned.MarkAsReturned(DateTime.UtcNow.AddDays(-1));
        var loans = new List<Loan> { active, returned };

        var result = new ActiveLoansSpec().Evaluate(loans).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeFalse_ForReturnedLoan()
    {
        var returned = new Loan(1, 1, DateTime.UtcNow.AddDays(-5));
        returned.MarkAsReturned(DateTime.UtcNow.AddDays(-1));

        new ActiveLoansSpec().IsSatisfiedBy(returned).Should().BeFalse();
    }
}
