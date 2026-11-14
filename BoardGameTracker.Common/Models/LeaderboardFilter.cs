using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models;

public record LeaderboardFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    int? GameId = null,
    int MinSessions = LeaderboardFilter.DefaultMinSessions,
    LeaderboardRankBy RankBy = LeaderboardRankBy.Wins,
    int? CategoryId = null)
{
    public const int DefaultMinSessions = 5;
}
