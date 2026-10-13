using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class RunningGame : HasId
{
    public int GameNightId { get; private set; }
    public GameNight GameNight { get; private set; } = null!;
    public int GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    public DateTime Start { get; private set; }
    public ICollection<RunningGamePlayer> Players { get; private set; } = [];

    public static RunningGame Create(int gameNightId, int gameId, DateTime start, IEnumerable<int> playerIds)
    {
        var playerIdList = playerIds.Distinct().ToList();
        Guard.Against.NullOrEmpty(playerIdList);

        return new RunningGame
        {
            GameNightId = Guard.Against.NegativeOrZero(gameNightId),
            GameId = Guard.Against.NegativeOrZero(gameId),
            Start = Guard.Against.Default(start),
            Players = playerIdList.Select(RunningGamePlayer.Create).ToList(),
        };
    }

    public List<int> GetPlayerIds()
    {
        return Players.Select(x => x.PlayerId).ToList();
    }
}
