using System.Xml.Serialization;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Bgg;

public class BggGame
{
    public string[] Names { get; set; }
    public int YearPublished { get; set; }
    public string Thumbnail { get; set; }
    public string Image { get; set; }
    public string Description { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int MinPlayTime { get; set; }
    public int MaxPlayTime { get; set; }
    public int MinAge { get; set; }
    public double Rating { get; set; }
    public double Weight { get; set; }
    public int BggId { get; set; }
    public BggLink[] Categories { get; set; }
    public BggLink[] Mechanics { get; set; }
    public BggPerson[] People { get; set; }
}

public class BggLink
{
    public string Value { get; set; }
    public int Id { get; set; }
}

public class BggPerson : BggLink
{
    public PersonType Type { get; set; }
}