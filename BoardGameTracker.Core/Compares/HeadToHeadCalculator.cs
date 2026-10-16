using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Core.Compares;

public static class HeadToHeadCalculator
{
    public const int RecentSessionCount = 10;
    public const int MinimumSessionsForBestAndWorst = 3;
    public const int BestGameCount = 3;
    public const int MinimumStreak = 2;

    public static CompareOutcome Outcome(SessionTogether session) => session.IsCoop
        ? CompareOutcome.SameTeam
        : (session.PlayerOneWon, session.PlayerTwoWon) switch
        {
            (true, true) => CompareOutcome.Both,
            (true, false) => CompareOutcome.PlayerOne,
            (false, true) => CompareOutcome.PlayerTwo,
            _ => CompareOutcome.Other
        };

    public static void Fill(CompareResultDto result, IReadOnlyList<SessionTogether> sessions, int playerOne, int playerTwo)
    {
        var games = Games(sessions);
        var sameTeam = sessions.Where(s => s.IsCoop).ToList();

        result.DirectWins = new CompareRow<int>(sessions.Count(PlayerOneBeat), sessions.Count(PlayerTwoBeat));
        result.WonByOthers = sessions.Count(s => Outcome(s) == CompareOutcome.Other);
        result.SameTeam = new CompareTeamRecord
        {
            Count = sameTeam.Count,
            Won = sameTeam.Count(s => s.TeamWon == true),
            Lost = sameTeam.Count(s => s.TeamWon != true)
        };
        result.TotalSessionsTogether = sessions.Count;
        result.MinutesPlayed = (int)sessions.Sum(Minutes);
        result.Games = games;
        result.PreferredGame = games.Count == 0
            ? null
            : new PreferredGame { GameId = games[0].GameId, SessionCount = games[0].SessionCount };
        result.LongestSessionTogether = Longest(sessions);
        result.FirstGameTogether = First(sessions);
        result.ClosestGame = Closest(sessions, playerOne, playerTwo);
        result.Streak = Streak(sessions);
        result.RecentSessions = Recent(sessions);
        result.BestGames = new CompareRow<List<int>>(BestGames(games, CompareOutcome.PlayerOne), BestGames(games, CompareOutcome.PlayerTwo));
    }

    public static List<CompareGame> Games(IEnumerable<SessionTogether> sessions)
    {
        return sessions
            .GroupBy(s => s.GameId)
            .Select(group =>
            {
                var last = group.OrderByDescending(s => s.Start).ThenByDescending(s => s.SessionId).First();
                return new CompareGame
                {
                    GameId = group.Key,
                    SessionCount = group.Count(),
                    PlayerOneWins = group.Count(PlayerOneBeat),
                    PlayerTwoWins = group.Count(PlayerTwoBeat),
                    OtherWins = group.Count(s => Outcome(s) == CompareOutcome.Other),
                    SameTeamCount = group.Count(s => s.IsCoop),
                    SameTeamWins = group.Count(s => s.IsCoop && s.TeamWon == true),
                    PlayerOneAverageScore = AverageScore(group, s => s.PlayerOneScore),
                    PlayerTwoAverageScore = AverageScore(group, s => s.PlayerTwoScore),
                    LastPlayed = last.Start,
                    LastOutcome = Outcome(last),
                    LastTeamWon = last.IsCoop ? last.TeamWon == true : null
                };
            })
            .OrderByDescending(game => game.SessionCount)
            .ThenByDescending(game => game.LastPlayed)
            .ThenBy(game => game.GameId)
            .ToList();
    }

    public static List<int> BestGames(IEnumerable<CompareGame> games, CompareOutcome side)
    {
        if (side is not (CompareOutcome.PlayerOne or CompareOutcome.PlayerTwo))
        {
            return [];
        }

        return games
            .Select(game => new { game.GameId, SessionCount = game.SessionCount - game.SameTeamCount, Lead = Lead(game, side) })
            .Where(game => game.SessionCount >= MinimumSessionsForBestAndWorst)
            .Where(game => game.Lead > 0)
            .OrderByDescending(game => game.Lead)
            .ThenByDescending(game => game.SessionCount)
            .ThenBy(game => game.GameId)
            .Take(BestGameCount)
            .Select(game => game.GameId)
            .ToList();
    }

    public static CompareStreak? Streak(IEnumerable<SessionTogether> sessionsNewestFirst)
    {
        CompareOutcome? side = null;
        var count = 0;

        foreach (var outcome in sessionsNewestFirst.Select(Outcome))
        {
            if (outcome is CompareOutcome.Both or CompareOutcome.SameTeam)
            {
                continue;
            }

            if (outcome == CompareOutcome.Other || (side != null && outcome != side))
            {
                break;
            }

            side = outcome;
            count++;
        }

        return side != null && count >= MinimumStreak ? new CompareStreak { Side = side.Value, Count = count } : null;
    }

    public static ClosestGame? Closest(IEnumerable<SessionTogether> sessions, int playerOne, int playerTwo)
    {
        var closest = sessions
            .Where(s => !s.IsCoop && s.HasScoring && s.PlayerOneScore.HasValue && s.PlayerTwoScore.HasValue)
            .OrderBy(s => Math.Abs(s.PlayerOneScore!.Value - s.PlayerTwoScore!.Value))
            .ThenByDescending(s => s.Start)
            .FirstOrDefault();

        if (closest == null)
        {
            return null;
        }

        return new ClosestGame
        {
            GameId = closest.GameId,
            ScoringDifference = Math.Abs(closest.PlayerOneScore!.Value - closest.PlayerTwoScore!.Value),
            PlayerId = closest.PlayerOneWon ? playerOne : closest.PlayerTwoWon ? playerTwo : null
        };
    }

    public static List<CompareSession> Recent(IEnumerable<SessionTogether> sessionsNewestFirst)
    {
        return sessionsNewestFirst
            .Take(RecentSessionCount)
            .Select(s => new CompareSession
            {
                SessionId = s.SessionId,
                GameId = s.GameId,
                Start = s.Start,
                Minutes = (int)Minutes(s),
                PlayerOneScore = s.PlayerOneScore,
                PlayerTwoScore = s.PlayerTwoScore,
                Outcome = Outcome(s),
                OtherWinnerId = s.OtherWinnerId,
                TeamWon = s.IsCoop ? s.TeamWon == true : null
            })
            .ToList();
    }

    private static LongestSession? Longest(IEnumerable<SessionTogether> sessions)
    {
        var longest = sessions.OrderByDescending(Minutes).ThenByDescending(s => s.Start).FirstOrDefault();
        return longest == null ? null : new LongestSession { GameId = longest.GameId, Minutes = (int)Minutes(longest) };
    }

    private static FirstGameTogether? First(IEnumerable<SessionTogether> sessions)
    {
        var first = sessions.OrderBy(s => s.Start).ThenBy(s => s.SessionId).FirstOrDefault();
        return first == null ? null : new FirstGameTogether { GameId = first.GameId, StartDate = first.Start };
    }

    private static int Lead(CompareGame game, CompareOutcome side) => side == CompareOutcome.PlayerOne
        ? game.PlayerOneWins - game.PlayerTwoWins
        : game.PlayerTwoWins - game.PlayerOneWins;

    private static bool PlayerOneBeat(SessionTogether session) => !session.IsCoop && session.PlayerOneWon;

    private static bool PlayerTwoBeat(SessionTogether session) => !session.IsCoop && session.PlayerTwoWon;

    private static double? AverageScore(IEnumerable<SessionTogether> sessions, Func<SessionTogether, double?> score)
    {
        var scores = sessions.Where(s => s.HasScoring && !s.IsCoop).Select(score).OfType<double>().ToList();
        return scores.Count == 0 ? null : Math.Round(scores.Average(), 1);
    }

    private static double Minutes(SessionTogether session) => (session.End - session.Start).TotalMinutes;
}
