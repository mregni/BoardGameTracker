namespace BoardGameTracker.Common.DTOs;

public class KeyValuePairDto<T>
{
    public string Key { get; set; }
    public T Value { get; set; }

    public KeyValuePairDto(string key, T value)
    {
        Key = key;
        Value = value;
    }
}