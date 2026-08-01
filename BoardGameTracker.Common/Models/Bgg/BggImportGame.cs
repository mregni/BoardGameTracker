using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Bgg;

public class BggImportGame
{
    public string Title { get; set; } = string.Empty;
    public int BggId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public GameState State { get; set; }
    public DateTime LastModified { get; set; }
}
