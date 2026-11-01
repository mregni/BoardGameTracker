namespace BoardGameTracker.Common.Models;

public class GamePlaySummary
{
    public int GameId { get; set; }
    public int PlayCount { get; set; }
    public DateTime? LastPlayed { get; set; }
}
