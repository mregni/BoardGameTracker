using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Loans.Interfaces;
using BoardGameTracker.Core.Loans.Specifications;
using BoardGameTracker.Core.Players.Specifications;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Loans;

public class LoanService : ILoanService
{
    private readonly IRepository<Loan> _loanRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IReadRepository<Player> _playerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoanService> _logger;

    public LoanService(
        IRepository<Loan> loanRepository,
        IGameRepository gameRepository,
        IReadRepository<Player> playerRepository,
        IUnitOfWork unitOfWork,
        ILogger<LoanService> logger)
    {
        _loanRepository = loanRepository;
        _gameRepository = gameRepository;
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<Loan>> GetLoans()
    {
        _logger.LogDebug("Fetching all loans");
        return _loanRepository.ListAsync(new LoansOrderedByDateSpec());
    }

    public Task<Loan?> GetLoanById(int id)
    {
        _logger.LogDebug("Fetching loan {LoanId}", id);
        return _loanRepository.GetByIdAsync(id);
    }

    public async Task<Loan> LoanGameToPlayer(CreateLoanCommand command)
    {
        _logger.LogDebug("Loaning game {GameId} to player {PlayerId}", command.GameId, command.PlayerId);
        var game = await _gameRepository.SingleOrDefaultAsync(new GameWithLoansSpec(command.GameId));
        if (game == null)
        {
            throw new EntityNotFoundException(nameof(Game), command.GameId);
        }

        if (!await _playerRepository.AnyAsync(new PlayerByIdSpec(command.PlayerId)))
        {
            throw new EntityNotFoundException(nameof(Player), command.PlayerId);
        }

        var loan = game.LoanToPlayer(command.PlayerId, command.LoanDate);
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

    public Task<int> CountActiveLoans(CancellationToken cancellationToken = default)
    {
        return _loanRepository.CountAsync(new ActiveLoansSpec(), cancellationToken);
    }
}
