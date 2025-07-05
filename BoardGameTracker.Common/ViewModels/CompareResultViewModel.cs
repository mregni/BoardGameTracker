using BoardGameTracker.Common.ViewModels.Compare;

namespace BoardGameTracker.Common.ViewModels;

public class CompareResultViewModel
{
    public CompareRowViewModel<int> DirectWins { get; set; }
    public CompareRowViewModel<MostWonGameViewModel?> MostWonGame { get; set; }
    public CompareRowViewModel<int> SessionCounts { get; set; }
    public CompareRowViewModel<int> WinCount { get; set; }
    public CompareRowViewModel<double> WinPercentageCount { get; set; }
    public CompareRowViewModel<double> TotalDuration { get; set; }
}

public class CompareRowViewModel<T>
{
    public T Left { get; set; }
    public T Right { get; set; }

    public CompareRowViewModel(T left, T right)
    {
        Left = left;
        Right = right;
    }
}