namespace BoardGameTracker.Common.ViewModels.Results;

public class FailResultViewModel
{
    public string Reason { get; set; }

    public FailResultViewModel(string reason)
    {
        Reason = reason;
    }
}