using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Common.Models;

public class CompareResult
{
    public CompareRow<int> DirectWins { get; set; }
    public CompareRow<MostWonGame?> MostWonGame { get; set; }
    public CompareRow<int> SessionCounts { get; set; }
    public CompareRow<int> WinCount { get; set; }
    public CompareRow<double> WinPercentageCount { get; set; }
    public CompareRow<double> TotalDuration { get; set; }
    
    //TODO: to implmeent
    // Top 3 game tags played

    public CompareResult()
    {
        DirectWins = new CompareRow<int>(0, 0);
        MostWonGame = new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame());
        SessionCounts = new CompareRow<int>(0, 0);
        WinCount = new CompareRow<int>(0, 0);
        TotalDuration = new CompareRow<double>(0, 0);
        WinPercentageCount = new CompareRow<double>(0, 0);
    }
}

public class CompareRow<T>
{
    public T Left { get; set; }
    public T Right { get; set; }

    public CompareRow(T left, T right)
    {
        Left = left;
        Right = right;
    }
}