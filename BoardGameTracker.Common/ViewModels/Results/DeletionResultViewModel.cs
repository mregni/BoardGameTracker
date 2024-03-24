using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels.Results;

public class DeletionResultViewModel
{
    public int Type { get; set; }

    public DeletionResultViewModel(ResultState state)
    {
        Type = (int)state;
    }
}