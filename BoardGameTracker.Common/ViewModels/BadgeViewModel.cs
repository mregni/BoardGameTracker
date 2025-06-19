namespace BoardGameTracker.Common.ViewModels;

public class BadgeViewModel
{
    public int Id { get; set; }    
    public string DescriptionKey { get; set; }
    public string TitleKey { get; set; }
    public int Type { get; set; }
    public int? Level { get; set; }
    public string Image { get; set; }
}