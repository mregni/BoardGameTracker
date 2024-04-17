using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class GameViewModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int? YearPublished { get; set; }
    public string Image { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? MinPlayTime { get; set; }
    public int? MaxPlayTime { get; set; }
    public int? MinAge { get; set; }
    public double? Rating { get; set; }
    public double? Weight { get; set; }
    public int? BggId { get; set; }
    public int Type { get; set; }
    public int State { get; set; }
    public int? BaseGameId { get; set; }
    public bool HasScoring { get; set; }
    public GameViewModel BaseGame { get; set; }
    public List<GameViewModel> Expansions { get; set; }
    public List<GameLinkViewModel> Categories { get; set; }
    public List<GameLinkViewModel> Mechanics { get; set; }
    public List<GamePersonViewModel> People { get; set; }
    public List<PlayViewModel> Plays { get; set; }
}

public class GameLinkViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class GamePersonViewModel : GameLinkViewModel
{
    public PersonType Type { get; set; }
}