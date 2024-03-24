using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class TopPlayerViewModel
{
    public int PlayerId { get; set; }
    public int PlayCount { get; set; }
    public int Wins { get; set; }
    public double WinPercentage { get; set; }
    public Trend Trend { get; set; }
}