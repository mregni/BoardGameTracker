using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class GameViewModel :CreateGameViewModel
{
    public int Id { get; set; }
    public double? Rating { get; set; }
    public double? Weight { get; set; }
    public int Type { get; set; }
    public string? BaseGameId { get; set; }
    public DateTime? AdditionDate { get; set; }
    public GameViewModel? BaseGame { get; set; }
    public List<GameViewModel>? Expansions { get; set; }
    public List<GameLinkViewModel>? Categories { get; set; }
    public List<GameLinkViewModel>? Mechanics { get; set; }
    public List<GamePersonViewModel>? People { get; set; }
    public List<SessionViewModel>? Sessions { get; set; }
}

public class GameLinkViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class GamePersonViewModel : GameLinkViewModel
{
    public PersonType Type { get; set; }
}