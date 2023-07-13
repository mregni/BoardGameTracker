using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class CreationResultViewModel<T> where T : class
{
    public int Type { get; set; }
    public string? Reason { get; set; }
    public T? Data { get; set; }

    public CreationResultViewModel(CreationResultType type, T? data)
    {
        Type = (int)type;
        Reason = string.Empty;
        Data = data;
    }

    public CreationResultViewModel(CreationResultType type, T? data, string reason)
    {
        Type = (int)type;
        Data = data;
        Reason = reason;
    }
}