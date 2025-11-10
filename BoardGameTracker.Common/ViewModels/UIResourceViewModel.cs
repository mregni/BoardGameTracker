namespace BoardGameTracker.Common.ViewModels;

public class UIResourceViewModel
{
    public required string DateFormat { get; set; }
    public required string TimeFormat { get; set; }
    public required string UILanguage { get; set; }
    public required string Currency { get; set; }
    public bool Statistics { get; set; }
}