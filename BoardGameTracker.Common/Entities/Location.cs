using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Location: HasId
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        private set => _name = Guard.Against.NullOrWhiteSpace(value, nameof(Name));
    }

    public ICollection<Session> Sessions { get; private set; }

    public Location(string name)
    {
        Name = name;
        Sessions = new List<Session>();
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public int GetPlayCount() => Sessions.Count;

    public IEnumerable<Game> GetGamesPlayedAtLocation()
    {
        return Sessions
            .Select(s => s.Game)
            .Distinct();
    }
}