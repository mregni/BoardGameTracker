namespace BoardGameTracker.Common.ViewModels.Location;

public class LocationViewModel : CreateLocationViewModel
{
    public required string Id { get; set; }
    public int PlayCount { get; set; }
}