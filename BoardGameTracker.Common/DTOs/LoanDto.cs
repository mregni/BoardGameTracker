namespace BoardGameTracker.Common.DTOs;

public class LoanDto
{
    public int Id { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public bool IsActive { get; set; }
}
