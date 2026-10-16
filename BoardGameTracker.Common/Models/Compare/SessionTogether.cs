namespace BoardGameTracker.Common.Models.Compare;

public class SessionTogether
{
    public int SessionId { get; init; }
    public int GameId { get; init; }
    public bool HasScoring { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public double? PlayerOneScore { get; init; }
    public bool PlayerOneWon { get; init; }
    public double? PlayerTwoScore { get; init; }
    public bool PlayerTwoWon { get; init; }
    public int? OtherWinnerId { get; init; }
}
