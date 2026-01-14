using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Image: HasId
{
    private string _path = string.Empty;

    public string Path
    {
        get => _path;
        private set => _path = Guard.Against.NullOrWhiteSpace(value, nameof(Path));
    }

    public int? GamePlayId { get; private set; }
    public Session? Play { get; private set; }
    public int GameId { get; private set; }
    public Game Game { get; private set; } = null!;

    public Image(string path, int gameId, int? gamePlayId = null)
    {
        Path = path;
        GameId = Guard.Against.NegativeOrZero(gameId);
        GamePlayId = gamePlayId;
    }
}