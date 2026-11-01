namespace BoardGameTracker.Common.DTOs;

public class GamePlaySummaryDto
{
    public int GameId { get; set; }
    public int PlayCount { get; set; }
    public DateTime? LastPlayed { get; set; }
}
