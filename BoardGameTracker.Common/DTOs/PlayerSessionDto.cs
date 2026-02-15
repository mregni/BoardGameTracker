namespace BoardGameTracker.Common.DTOs;

public class PlayerSessionDto
{
    public int PlayerId { get; set; }
    public int SessionId { get; set; }
    public double? Score { get; set; }
    public bool FirstPlay { get; set; }
    public bool Won { get; set; }
}
