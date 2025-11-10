using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class ImportGameListViewModal
{
    public List<ImportGameViewModel> Games { get; set; } = [];
}
public class ImportGameViewModel
{
    public required string Title { get; set; }
    public int BggId { get; set; }
    public required string ImageUrl { get; set; }
    public bool IsExpansion { get; set; }
    public GameState State { get; set; }
    public bool HasScoring { get; set; }
    public double Price { get; set; }
    public DateTime AddedDate { get; set; }
}