namespace BoardGameTracker.Common.ViewModels;

public class PlayerViewModel : PlayerCreationViewModel
{
    public required string Id { get; set; }
    public List<BadgeViewModel> Badges { get; set; } = [];

}