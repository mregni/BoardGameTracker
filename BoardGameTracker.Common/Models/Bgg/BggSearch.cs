using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Bgg;

public class BggSearch
{
    public int BggId { get; set; }
    public GameState State { get; set; }
    public double? Price { get; set; }
    public DateTime? AdditionDate { get; set; }
}