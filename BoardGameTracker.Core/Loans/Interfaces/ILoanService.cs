using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Loans.Interfaces;

public interface ILoanService
{
    Task<List<Loan>> GetLoans();
    Task<Loan?> GetLoanById(int id);
    Task<Loan> Create(Loan loan);
    Task<Loan> Update(Loan loan);
    Task Delete(int id);
}
