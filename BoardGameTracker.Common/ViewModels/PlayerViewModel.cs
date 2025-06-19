namespace BoardGameTracker.Common.ViewModels;

public class PlayerViewModel : PlayerCreationViewModel
{
    public string Id { get; set; }
    public List<BadgeViewModel> Badges { get; set; }
    
}