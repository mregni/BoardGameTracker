using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Loans.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Loans;

public class LoansOrderedByDateSpecTests
{
    [Fact]
    public void Evaluate_ShouldOrderByLoanDateDescending()
    {
        var oldest = new Loan(1, 1, DateTime.UtcNow.AddDays(-10)) { Id = 1 };
        var newest = new Loan(2, 1, DateTime.UtcNow.AddDays(-1)) { Id = 2 };
        var middle = new Loan(3, 1, DateTime.UtcNow.AddDays(-5)) { Id = 3 };
        var loans = new List<Loan> { oldest, newest, middle };

        var result = new LoansOrderedByDateSpec().Evaluate(loans).ToList();

        result.Select(x => x.Id).Should().Equal(2, 3, 1);
    }

    [Fact]
    public void Spec_ShouldBeNoTracking()
    {
        new LoansOrderedByDateSpec().AsNoTracking.Should().BeTrue();
    }
}
