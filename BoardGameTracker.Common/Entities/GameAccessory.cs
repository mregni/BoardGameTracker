using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class GameAccessory : HasId
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Game Game { get; set; } = null!;
    public int GameId { get; set; }
}