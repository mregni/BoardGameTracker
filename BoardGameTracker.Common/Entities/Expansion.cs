using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Expansion : BaseGame
{
    public Game BaseGame { get; set; }
    public int? BaseGameId { get; set; }
}