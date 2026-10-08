namespace BoardGameTracker.Common.Models.Charts;

public class PlayerScoringPoint
{
    public DateTime DateTime { get; init; }
    public XValue[] Series { get; init; } = [];
}
