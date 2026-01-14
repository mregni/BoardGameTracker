using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.Entities.Helpers;

public class PlayerSession
{
    public int PlayerId { get; private set; }
    public Player Player { get; private set; } = null!;
    public int SessionId { get; private set; }
    public Session Session { get; private set; } = null!;
    public double? Score { get; private set; }
    public bool FirstPlay { get; private set; }
    public bool Won { get; private set; }

    public PlayerSession(int playerId, double? score = null, bool firstPlay = false, bool won = false)
    {
        Guard.Against.NegativeOrZero(playerId);

        PlayerId = playerId;
        Score = score;
        FirstPlay = firstPlay;
        Won = won;
    }

    public void UpdateScore(double? score)
    {
        Score = score;
    }

    public void MarkAsFirstPlay()
    {
        FirstPlay = true;
    }

    public void MarkAsWinner()
    {
        Won = true;
    }

    public void MarkAsLoser()
    {
        Won = false;
    }
}