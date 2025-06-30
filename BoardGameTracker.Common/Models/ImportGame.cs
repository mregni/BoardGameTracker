using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models;

public class ImportGame
{
    public string Title { get; set; }
    public int BggId { get; set; }
    public string ImageUrl { get; set; }
    public GameState State { get; set; }
    public bool HasScoring { get; set; }
    public double Price { get; set; }
    public DateTime AddedDate { get; set; }
}