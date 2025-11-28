namespace BoardGameTracker.Common.ViewModels;

public class CreateLoanViewModel
{
    public required string GameId { get; set; }
    public required string PlayerId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    
}