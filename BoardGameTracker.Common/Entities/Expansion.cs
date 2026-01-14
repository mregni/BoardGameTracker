using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.ValueObjects;

namespace BoardGameTracker.Common.Entities;

public class Expansion : HasId
{
    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        private set => _title = Guard.Against.NullOrWhiteSpace(value, nameof(Title));
    }

    public int BggId { get; private set; }

    [JsonIgnore]
    public Game Game { get; private set; } = null!;
    public int GameId { get; private set; }
    public ICollection<Session> Sessions { get; private set; }

    public Expansion(string title, int bggId, int gameId)
    {
        Title = title;
        BggId = Guard.Against.NegativeOrZero(bggId);
        GameId = Guard.Against.NegativeOrZero(gameId);
        Sessions = new List<Session>();
    }
}