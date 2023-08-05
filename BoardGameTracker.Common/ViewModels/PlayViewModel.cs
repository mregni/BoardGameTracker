namespace BoardGameTracker.Common.ViewModels;

public class PlayViewModel
{
    public string? Comment { get; set; }
    public bool Ended { get; set; }
    public int GameId { get; set; }
    public List<PlayerPlayViewModel> Players { get; set; }
    public List<GameSessionViewModel> Sessions { get; set; }
}

public class GameSessionViewModel
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class PlayerPlayViewModel
{
    public int PlayerId { get; set; }
    public bool Won { get; set; }
    public bool FirstPlay { get; set; }
    public string? Color { get; set; }
    public int? Score { get; set; }
    public string? Team { get; set; }
    public string? CharacterName { get; set; }
}