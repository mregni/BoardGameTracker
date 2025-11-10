using BoardGameTracker.Common.ViewModels.Compare;

namespace BoardGameTracker.Common.ViewModels;

public class CompareResultViewModel
{
    public CompareRowViewModel<int> DirectWins { get; set; } = null!;
    public CompareRowViewModel<MostWonGameViewModel?> MostWonGame { get; set; } = null!;
    public CompareRowViewModel<int> SessionCounts { get; set; } = null!;
    public CompareRowViewModel<int> WinCount { get; set; } = null!;
    public CompareRowViewModel<double> WinPercentageCount { get; set; } = null!;
    public CompareRowViewModel<double> TotalDuration { get; set; } = null!;
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