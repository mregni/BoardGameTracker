using BoardGameTracker.Common;
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

        topPlayer.Trend = CalculateTrend(play);
        return topPlayer;
    }

    private static Trend CalculateTrend(IEnumerable<PlayerSession> play)
    {
        var ordered = play
            .OrderByDescending(x => x.Session.Start)
            .ToList();
        var recent = ordered.Take(Constants.Game.TrendWindowSize).ToList();
        var previous = ordered.Skip(Constants.Game.TrendWindowSize).ToList();
        if (previous.Count == 0)
        {
            return Trend.Equal;
        }

        var recentWinRate = recent.Count(x => x.Won) / (double)recent.Count;
        var previousWinRate = previous.Count(x => x.Won) / (double)previous.Count;
        if (recentWinRate > previousWinRate)
        {
            return Trend.Up;
        }

        return recentWinRate < previousWinRate ? Trend.Down : Trend.Equal;
    }
}