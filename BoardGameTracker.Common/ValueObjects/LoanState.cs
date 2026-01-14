namespace BoardGameTracker.Common.ValueObjects;

public enum LoanStateType
{
    Active,
    Returned,
    Overdue
}

public record LoanState
{
    public LoanStateType State { get; }
    public DateTime? DueDate { get; }

    private LoanState(LoanStateType state, DateTime? dueDate = null)
    {
        State = state;
        DueDate = dueDate;
    }
}
