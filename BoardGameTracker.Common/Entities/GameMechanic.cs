using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class GameMechanic : HasId
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        private set => _name = Guard.Against.NullOrWhiteSpace(value, nameof(Name));
    }

    public ICollection<Game> Games { get; private set; }

    public GameMechanic(string name)
    {
        Name = name;
        Games = new List<Game>();
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public int GetGameCount() => Games.Count;
}