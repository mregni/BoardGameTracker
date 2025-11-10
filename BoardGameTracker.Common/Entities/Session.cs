using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Session: HasId
{
    public string Comment { get; set; } = string.Empty;
    public int GameId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Game Game { get; set; } = null!;
    public ICollection<Expansion> Expansions { get; set; } = [];
    public int? LocationId { get; set; }
    public Location? Location { get; set; }
    public ICollection<Image> ExtraImages { get; set; } = [];
    public ICollection<PlayerSession> PlayerSessions { get; set; } = [];
}