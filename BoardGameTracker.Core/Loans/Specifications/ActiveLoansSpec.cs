using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Loans.Specifications;

public sealed class ActiveLoansSpec : Specification<Loan>
{
    public ActiveLoansSpec(DateTime now)
    {
        Query.Where(x => x.ReturnedDate == null && x.LoanDate <= now);
    }
}
