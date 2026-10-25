using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Compare;

public class CompareGame
{
    public int GameId { get; set; }
    public int SessionCount { get; set; }
    public int PlayerOneWins { get; set; }
    public int PlayerTwoWins { get; set; }
    public int OtherWins { get; set; }
    public int SameTeamCount { get; set; }
    public int SameTeamWins { get; set; }
    public double? PlayerOneAverageScore { get; set; }
    public double? PlayerTwoAverageScore { get; set; }
    public int ScoredSessionCount { get; set; }
    public DateTime LastPlayed { get; set; }
    public CompareOutcome LastOutcome { get; set; }
    public bool? LastTeamWon { get; set; }
}
