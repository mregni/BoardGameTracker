using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models;

public class TopPlayer
{
    public int PlayerId { get; set; }
    public int PlayCount { get; set; }
    public int Wins { get; set; }
    public double WinPercentage { get; set; }
    public Trend Trend { get; set; }

    public static TopPlayer CreateTopPlayer(IGrouping<int?, PlayerPlay> play)
    {
        var topPlayer = new TopPlayer
        {
            PlayerId = play.First().PlayerId.Value,
            PlayCount = play.Count(),
            Wins = play.Count(x => x.Won),
            WinPercentage = play.Count(x => x.Won) / (double)play.Count()
        };

        var playList = play
            .OrderByDescending(x => x.Play.Start)
            .ToList();
        playList.RemoveAt(0);
        var previousWinRate = playList.Count(x => x.Won) / (double)playList.Count();
        
        if (topPlayer.WinPercentage > previousWinRate)
        {
            topPlayer.Trend = Trend.Up;
        } else if (topPlayer.WinPercentage < previousWinRate)
        {
            topPlayer.Trend = Trend.Down;
        }
        else
        {
            topPlayer.Trend = Trend.Equal;
        }

        return topPlayer;
    }
}