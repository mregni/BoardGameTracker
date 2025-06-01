namespace BoardGameTracker.Common.ViewModels;

public class PlayerSessionViewModel
{
    public string PlayerId { get; set; }
    public bool Won { get; set; }
    public bool FirstPlay { get; set; }
    public double? Score { get; set; }
}