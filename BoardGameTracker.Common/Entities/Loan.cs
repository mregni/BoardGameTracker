using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Loan : HasId
{
    public DateTime LoanDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? ReturnedDate { get; private set; }
    public int GameId { get; private set; }
    public Game Game { get; private set; }
    public int PlayerId { get; private set; }
    public Player Player { get; private set; }

    public Loan(int gameId, int playerId, DateTime loanDate)
    {
        GameId = Guard.Against.Null(gameId);
        PlayerId = Guard.Against.Null(playerId);

        LoanDate = loanDate;
    }

    public void MarkAsReturned(DateTime returnedDate)
    {
        Guard.Against.Null(returnedDate);

        ValidateReturnDate(returnedDate, LoanDate);

        if (ReturnedDate != null)
            throw new InvalidOperationException("Loan has already been returned.");

        ReturnedDate = returnedDate;
    }

    public bool IsCurrentlyOnLoan()
    {
        var now = DateTime.UtcNow;
        return LoanDate <= now && (ReturnedDate == null || now < ReturnedDate.Value);
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
            throw new ArgumentException("Due date cannot be before loan date.", nameof(dueDate));
        }
    }

    private static void ValidateReturnDate(DateTime returnedDate, DateTime loanDate)
    {
        if (returnedDate < loanDate)
        {
            throw new ArgumentException("Return date cannot be before loan date.", nameof(returnedDate));
        }
    }
}