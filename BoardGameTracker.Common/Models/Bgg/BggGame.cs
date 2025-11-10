using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Bgg;

public class BggGame
{
    public string[] Names { get; set; } = [];
    public int YearPublished { get; set; }
    public required string Thumbnail { get; set; }
    public required string Image { get; set; }
    public required string Description { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int MinPlayTime { get; set; }
    public int MaxPlayTime { get; set; }
    public int MinAge { get; set; }
    public double Rating { get; set; }
    public double Weight { get; set; }
    public int BggId { get; set; }
    public BggLink[] Categories { get; set; } = [];
    public BggLink[] Mechanics { get; set; } = [];
    public BggLink[] Expansions { get; set; } = [];
    public BggPerson[] People { get; set; } = [];
}

public class BggLink
{
    public required string Value { get; set; }
    public int Id { get; set; }
}

public class BggPerson : BggLink
{
    public PersonType Type { get; set; }
}