using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Expansion : HasId
{
    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        private set => _title = Guard.Against.NullOrWhiteSpace(value);
    }

    public int? BggId { get; private set; }

    [JsonIgnore]
    public Game Game { get; private set; } = null!;
    public int GameId { get; private set; }
    public ICollection<Session> Sessions { get; private set; }

    public Expansion(string title, int? bggId, int gameId)
    {
        Title = title;
        BggId = bggId == null ? null : Guard.Against.NegativeOrZero(bggId.Value);
        GameId = Guard.Against.NegativeOrZero(gameId);
        Sessions = new List<Session>();
    }

    public bool IsManual => BggId == null;

    public bool Matches(Expansion other)
    {
        return BggId != null
            ? BggId == other.BggId
            : other.BggId == null && string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    }
}