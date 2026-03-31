using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Loans.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Loans;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoanService> _logger;

    public LoanService(ILoanRepository loanRepository, IGameRepository gameRepository, IUnitOfWork unitOfWork, ILogger<LoanService> logger)
    {
        _loanRepository = loanRepository;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<Loan>> GetLoans()
    {
        _logger.LogDebug("Fetching all loans");
        return _loanRepository.GetAllAsync();
    }

    public Task<Loan?> GetLoanById(int id)
    {
        _logger.LogDebug("Fetching loan {LoanId}", id);
        return _loanRepository.GetByIdAsync(id);
    }

    public async Task<Loan> LoanGameToPlayer(CreateLoanCommand command)
    {
        _logger.LogDebug("Loaning game {GameId} to player {PlayerId}", command.GameId, command.PlayerId);
        var game = await _gameRepository.GetByIdAsync(command.GameId);
        if (game == null)
        {
            throw new EntityNotFoundException(nameof(Game), command.GameId);
        }

        var loan = game.LoanToPlayer(command.PlayerId, command.LoanDate, command.DueDate);
        loan.SetDueDate(command.DueDate);

        await _loanRepository.CreateAsync(loan);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Loan {LoanId} created for game {GameId}", loan.Id, command.GameId);

        return loan;
    }

    public async Task<Loan> ReturnLoan(ReturnLoanCommand command)
    {
        _logger.LogDebug("Returning loan {LoanId}", command.Id);
        var loan = await _loanRepository.GetByIdAsync(command.Id);
        if (loan == null)
        {
            throw new EntityNotFoundException(nameof(Loan), command.Id);
        }

        loan.MarkAsReturned(command.ReturnDate);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Loan {LoanId} returned", command.Id);

        return loan;
    }

    public async Task<Loan> Update(UpdateLoanCommand command)
    {
        _logger.LogDebug("Updating loan {LoanId}", command.Id);
        var loan = await _loanRepository.GetByIdAsync(command.Id);
        if (loan == null)
        {
            throw new EntityNotFoundException(nameof(Loan), command.Id);
        }

        loan.UpdateDates(command.LoanDate, command.DueDate, loan.ReturnedDate);
        await _unitOfWork.SaveChangesAsync();
        
        return loan;
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting loan {LoanId}", id);
        await _loanRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public Task<int> CountActiveLoans()
    {
        return _loanRepository.CountActiveLoans();
    }
}
