namespace BoardGameTracker.Common.ViewModels;

public class BadgeViewModel
{
    public int Id { get; set; }
    public required string DescriptionKey { get; set; }
    public required string TitleKey { get; set; }
    public int Type { get; set; }
    public int? Level { get; set; }
    public required string Image { get; set; }
}