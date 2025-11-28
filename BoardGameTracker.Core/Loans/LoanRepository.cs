using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Loans.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Loans;

public class LoanRepository : CrudHelper<Loan>, ILoanRepository
{
    private readonly MainDbContext _context;

    public LoanRepository(MainDbContext context) : base(context)
    {
        _context = context;
    }

    public override Task<List<Loan>> GetAllAsync()
    {
        return _context.Loans
            .OrderByDescending(x => x.LoanDate)
            .ToListAsync();
    }
}
