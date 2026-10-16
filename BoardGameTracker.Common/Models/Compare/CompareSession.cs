using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Compare;

public class CompareSession
{
    public int SessionId { get; set; }
    public int GameId { get; set; }
    public DateTime Start { get; set; }
    public int Minutes { get; set; }
    public double? PlayerOneScore { get; set; }
    public double? PlayerTwoScore { get; set; }
    public CompareOutcome Outcome { get; set; }
    public int? OtherWinnerId { get; set; }
}
