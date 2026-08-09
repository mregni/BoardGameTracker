using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Loans.Specifications;

public sealed class LoansOrderedByDateSpec : Specification<Loan>
{
    public LoansOrderedByDateSpec()
    {
        Query
            .OrderByDescending(x => x.LoanDate)
            .AsNoTracking();
    }
}
