using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Common.Models;

public class CompareResultDto
{
    public CompareRow<int> WinCount { get; set; }
    public CompareRow<double> WinPercentage { get; set; }
    public CompareRow<int> SessionCounts { get; set; }
    public CompareRow<double> TotalDuration { get; set; }
    public CompareRow<int> DirectWins { get; set; }
    public int WonByOthers { get; set; }
    public int TotalSessionsTogether { get; set; }
    public int MinutesPlayed { get; set; }
    public PreferredGame? PreferredGame { get; set; }
    public LongestSession? LongestSessionTogether { get; set; }
    public FirstGameTogether? FirstGameTogether { get; set; }
    public ClosestGame? ClosestGame { get; set; }
    public CompareStreak? Streak { get; set; }
    public List<CompareGame> Games { get; set; }
    public List<CompareSession> RecentSessions { get; set; }
    public CompareRow<List<int>> BestGames { get; set; }

    public CompareResultDto()
    {
        DirectWins = new CompareRow<int>(0, 0);
        SessionCounts = new CompareRow<int>(0, 0);
        WinCount = new CompareRow<int>(0, 0);
        TotalDuration = new CompareRow<double>(0, 0);
        WinPercentage = new CompareRow<double>(0, 0);
        Games = [];
        RecentSessions = [];
        BestGames = new CompareRow<List<int>>([], []);
    }
}

public class CompareRow<T>
{
    public T PlayerOne { get; set; }
    public T PlayerTwo { get; set; }

    public CompareRow(T playerOne, T playerTwo)
    {
        PlayerOne = playerOne;
        PlayerTwo = playerTwo;
    }
}
