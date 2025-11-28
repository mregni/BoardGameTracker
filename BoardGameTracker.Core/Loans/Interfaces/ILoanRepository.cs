using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Loans.Interfaces;

public interface ILoanRepository : ICrudHelper<Loan>
{
}
