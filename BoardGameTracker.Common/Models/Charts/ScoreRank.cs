using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Models.Charts;

public class ScoreRank
{
    public string Key { get; set; }
    public double Score { get; set; }
    public int PlayerId { get; set; }

    public static ScoreRank? MakeHighestScoreRank(PlayerSession? play)
    {
        return Make(play, "top-score");
    }
    
    public static ScoreRank? MakeHighestLosingRank(PlayerSession? play)
    {
        return Make(play, "highest-losing");
    }
    
    public static ScoreRank? MakeLowestWinningRank(PlayerSession? play)
    {
        return Make(play, "lowest-winning");
    }
    
    public static ScoreRank? MakeLowestScoreRank(PlayerSession? play)
    {
        return Make(play, "lowest");
    }
    
    public static ScoreRank? MakeAverageRank(double? average)
    {
        if (!average.HasValue)
        {
            return null;
        }
        
        return new ScoreRank
        {
            Key = "average",
            Score = average.Value
        };
    }

    private static ScoreRank? Make(PlayerSession? playerSession, string key)
    {
        if (playerSession == null)
        {
            return null;
        }
        
        return new ScoreRank
        {
            Key = key,
            Score = playerSession.Score ?? 0,
            PlayerId = playerSession.PlayerId
        };
    }
}