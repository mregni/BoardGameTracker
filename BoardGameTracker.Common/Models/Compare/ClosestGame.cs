namespace BoardGameTracker.Common.Models.Compare;

public class ClosestGame
{
    public int? PlayerId { get; set; }
    public bool BothWon { get; set; }
    public bool OtherWon { get; set; }
    public int? GameId { get; set; }
    public double ScoringDifference { get; set; }
}
