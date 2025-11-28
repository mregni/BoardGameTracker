using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Loans.Interfaces;

namespace BoardGameTracker.Core.Loans;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;

    public LoanService(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public Task<List<Loan>> GetLoans()
    {
        return _loanRepository.GetAllAsync();
    }

    public Task<Loan?> GetLoanById(int id)
    {
        return _loanRepository.GetByIdAsync(id);
    }

    public Task<Loan> Create(Loan loan)
    {
        return _loanRepository.CreateAsync(loan);
    }

    public Task<Loan> Update(Loan loan)
    {
        return _loanRepository.UpdateAsync(loan);
    }

    public Task Delete(int id)
    {
        return _loanRepository.DeleteAsync(id);
    }
}
