using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Loan : HasId
{
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public required string GameId { get; set; }
    public Game Game { get; set; }
    public required string PlayerId { get; set; }
    public Player Player { get; set; }
}