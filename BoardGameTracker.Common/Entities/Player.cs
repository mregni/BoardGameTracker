using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Player : HasId
{
    public string Name { get; set; }
    public string? Image { get; set; }
    public ICollection<PlayerPlay> Plays { get; set; }
}