using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Common.Models;

public class PlayerComparison
{
    public int PlayerOneId { get; set; }
    public int PlayerTwoId { get; set; }
    public CompareRow<int> SessionCounts { get; set; } = null!;
    public CompareRow<double> TotalDuration { get; set; } = null!;
    public CompareRow<int> WinCount { get; set; } = null!;
    public CompareRow<double> WinPercentage { get; set; } = null!;
    public CompareRow<int> DirectWins { get; set; } = null!;
    public CompareRow<MostWonGame?> MostWonGame { get; set; } = null!;
}
