using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities.Helpers;

public class PlayerSession
{
    public int PlayerId { get; set; }
    public Player Player { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; }

    public double? Score { get; set; }
    public bool FirstPlay { get; set; }
    public bool Won { get; set; }
}