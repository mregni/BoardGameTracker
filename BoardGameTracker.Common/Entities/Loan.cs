using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Exceptions;

namespace BoardGameTracker.Common.Entities;

public class Loan : HasId
{
    public DateTime LoanDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? ReturnedDate { get; private set; }
    public int GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    public int PlayerId { get; private set; }
    public Player Player { get; private set; } = null!;

    public Loan(int gameId, int playerId, DateTime loanDate)
    {
        GameId = Guard.Against.Negative(gameId);
        PlayerId = Guard.Against.NegativeOrZero(playerId);

        LoanDate = loanDate;
    }

    public void MarkAsReturned(DateTime returnedDate)
    {
        Guard.Against.Null(returnedDate);

        ValidateReturnDate(returnedDate, LoanDate);

        if (ReturnedDate != null)
        {
            throw new DomainException(Constants.Errors.LoanAlreadyReturned);
        }

        ReturnedDate = returnedDate;
    }

    public bool IsCurrentlyOnLoan()
    {
        var now = DateTime.UtcNow;
        return LoanDate <= now && (ReturnedDate == null || now < ReturnedDate.Value);
    }

    public bool IsActiveOn(DateTime date)
    {
        if (LoanDate > date)
        {
            return false;
        }

        var effectiveEnd = ReturnedDate ?? DueDate;
        return effectiveEnd == null || date < effectiveEnd.Value;
    }

    public bool Overlaps(DateTime start, DateTime? end)
    {
        var effectiveEnd = ReturnedDate ?? DueDate;
        return (end == null || LoanDate < end.Value) && (effectiveEnd == null || start < effectiveEnd.Value);
    }

    public void SetDueDate(DateTime? dueDate)
    {
        if (dueDate.HasValue)
        {
            ValidateDueDate(dueDate.Value, LoanDate);
        }

        DueDate = dueDate;
    }

    public void UpdateDates(DateTime loanDate, DateTime? dueDate, DateTime? returnedDate)
    {
        if (dueDate.HasValue)
        {
            ValidateDueDate(dueDate.Value, loanDate);
        }

        if (returnedDate.HasValue)
        {
            ValidateReturnDate(returnedDate.Value, loanDate);
        }

        LoanDate = loanDate;
        DueDate = dueDate;
        ReturnedDate = returnedDate;
    }

    private static void ValidateDueDate(DateTime dueDate, DateTime loanDate)
    {
        if (dueDate < loanDate)
        {
            throw new DomainException(Constants.Errors.LoanDueBeforeStart);
        }
    }

    private static void ValidateReturnDate(DateTime returnedDate, DateTime loanDate)
    {
        if (returnedDate < loanDate)
        {
            throw new DomainException(Constants.Errors.LoanReturnedBeforeStart);
        }
    }
}