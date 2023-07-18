using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class GameSession: HasId
{
    public int GamePlayId { get; set; }
    public Play Play { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}