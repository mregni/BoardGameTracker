using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Loans.Interfaces;

public interface ILoanService
{
    Task<List<Loan>> GetLoans();
    Task<Loan?> GetLoanById(int id);
    Task<Loan?> Update(UpdateLoanCommand command);
    Task Delete(int id);
    Task<Loan> LoanGameToPlayer(CreateLoanCommand command);
    Task<Loan?> ReturnLoan(ReturnLoanCommand command);
}
