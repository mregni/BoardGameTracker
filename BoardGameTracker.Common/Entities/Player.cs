using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Player : HasId
{
    public required string Name { get; set; }
    public string? Image { get; set; }
    public ICollection<PlayerSession> PlayerSessions { get; set; } = [];
    public ICollection<Badge> Badges { get; set; } = [];
}