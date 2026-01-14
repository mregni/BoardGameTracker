namespace BoardGameTracker.Core.Bgg.AntiCorruption;

public class GameImportData
{
    public required string Title { get; set; }
    public int BggId { get; set; }
    public required string Description { get; set; }
    public int YearPublished { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int MinPlayTime { get; set; }
    public int MaxPlayTime { get; set; }
    public int MinAge { get; set; }
    public double Rating { get; set; }
    public double Weight { get; set; }
    public required string ImageUrl { get; set; }
    public IEnumerable<CategoryData> Categories { get; set; } = [];
    public IEnumerable<MechanicData> Mechanics { get; set; } = [];
    public IEnumerable<PersonData> People { get; set; } = [];
    public IEnumerable<ExpansionData> Expansions { get; set; } = [];
}

public class CategoryData
{
    public required string Name { get; set; }
    public int BggId { get; set; }
}

public class MechanicData
{
    public required string Name { get; set; }
    public int BggId { get; set; }
}

public class PersonData
{
    public required string Name { get; set; }
    public required string Type { get; set; }
}

public class ExpansionData
{
    public required string Title { get; set; }
    public int BggId { get; set; }
}
