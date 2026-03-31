namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateLoanCommand
{
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateLoanCommand : CreateLoanCommand
{
    public int Id { get; set; }
}

public class ReturnLoanCommand
{
    public int Id { get; set; }
    public DateTime ReturnDate { get; set; }
}
