using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Models.Charts;

public class ScoreRank
{
    public string Key { get; set; }
    public double Score { get; set; }
    public int? PlayerId { get; set; }

    public static ScoreRank? MakeHighestScoreRank(PlayerPlay? play)
    {
        return Make(play, "top-score");
    }
    
    public static ScoreRank? MakeHighestLosingRank(PlayerPlay? play)
    {
        return Make(play, "highest-losing");
    }
    
    public static ScoreRank? MakeLowestWinningRank(PlayerPlay? play)
    {
        return Make(play, "lowest-winning");
    }
    
    public static ScoreRank? MakeLowestScoreRank(PlayerPlay? play)
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

    private static ScoreRank? Make(PlayerPlay? play, string key)
    {
        if (play == null)
        {
            return null;
        }
        
        return new ScoreRank
        {
            Key = key,
            Score = play.Score ?? 0,
            PlayerId = play.PlayerId
        };
    }
}