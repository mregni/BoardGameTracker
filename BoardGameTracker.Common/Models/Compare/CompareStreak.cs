using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Compare;

public class CompareStreak
{
    public CompareOutcome Side { get; set; }
    public int Count { get; set; }
}
