namespace BoardGameTracker.Common.ViewModels;

public class KeyValuePairViewModel<T1, T2>
{
    public string Key { get; set; }
    public T2 Value { get; set; }

    public KeyValuePairViewModel(string key, T2 value)
    {
        Key = key;
        Value = value;
    }
}