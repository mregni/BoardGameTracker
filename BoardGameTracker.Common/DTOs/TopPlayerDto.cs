using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class TopPlayerDto
{
    public int PlayerId { get; private set; }
    public int PlayCount { get; private set; }
    public int Wins { get; private set; }
    public double WinPercentage { get; private set; }
    public double? AverageScore { get; private set; }
    public Trend Trend { get; private set; }

    public static TopPlayerDto CreateTopPlayer(IGrouping<int, PlayerSession> play)
    {
        var topPlayer = new TopPlayerDto
        {
            PlayerId = play.First().PlayerId,
            PlayCount = play.Count(),
            Wins = play.Count(x => x.Won),
            WinPercentage = play.Count(x => x.Won) / (double)play.Count(),
            AverageScore =  play.Average(x => x.Score),
        };

        var playList = play
            .OrderByDescending(x => x.Session.Start)
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