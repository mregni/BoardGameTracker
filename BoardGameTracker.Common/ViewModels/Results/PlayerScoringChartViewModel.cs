using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Common.ViewModels.Results;

public class PlayerScoringChartViewModel
{
    public DateTime DateTime { get; set; }
    public XValue[] Series { get; set; }
}