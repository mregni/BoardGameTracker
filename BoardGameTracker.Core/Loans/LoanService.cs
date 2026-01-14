using Ardalis.GuardClauses;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Loans.Interfaces;

namespace BoardGameTracker.Core.Loans;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanService(ILoanRepository loanRepository, IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<List<Loan>> GetLoans()
    {
        return _loanRepository.GetAllAsync();
    }

    public Task<Loan?> GetLoanById(int id)
    {
        return _loanRepository.GetByIdAsync(id);
    }

    public async Task<Loan> LoanGameToPlayer(CreateLoanCommand command)
    {
        var game = await _gameRepository.GetByIdAsync(command.GameId);
        Guard.Against.Null(game);

        var loan = game.LoanToPlayer(command.PlayerId, command.LoanDate, command.DueDate);
        loan.SetDueDate(command.DueDate);

        await _loanRepository.CreateAsync(loan);
        await _gameRepository.UpdateAsync(game);
        
        await _unitOfWork.SaveChangesAsync();

        return loan;
    }

    public async Task<Loan?> ReturnLoan(ReturnLoanCommand command)
    {
        var loan = await _loanRepository.GetByIdAsync(command.Id);
        if (loan == null)
        {
            return null;
        }

        loan.MarkAsReturned(command.ReturnDate);
        await _loanRepository.UpdateAsync(loan);
        await _unitOfWork.SaveChangesAsync();
        
        return loan;
    }

    public async Task<Loan?> Update(UpdateLoanCommand command)
    {
        var loan = await _loanRepository.GetByIdAsync(command.Id);
        if (loan == null)
        {
            return null;
        }

        loan.UpdateDates(command.LoanDate, command.DueDate, loan.ReturnedDate);
        await _loanRepository.UpdateAsync(loan);
        await _unitOfWork.SaveChangesAsync();
        
        return loan;
    }

    public async Task Delete(int id)
    {
        await _loanRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }
}
