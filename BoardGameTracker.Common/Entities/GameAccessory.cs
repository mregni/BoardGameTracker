using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class GameAccessory : HasId
{
    private string _title = string.Empty;
    private string _description = string.Empty;

    public string Title
    {
        get => _title;
        private set => _title = Guard.Against.NullOrWhiteSpace(value, nameof(Title));
    }

    public string Description
    {
        get => _description;
        private set => _description = Guard.Against.NullOrWhiteSpace(value, nameof(Description));
    }

    public Game Game { get; private set; } = null!;
    public int GameId { get; private set; }

    public GameAccessory(string title, string description, int gameId)
    {
        Title = title;
        Description = description;
        GameId = Guard.Against.NegativeOrZero(gameId);
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
    }
}