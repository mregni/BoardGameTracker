using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Config: HasId
{
    private string _key;

    public string Key
    {
        get => _key;
        set => _key = value.ToLowerInvariant();
    }
    public string Value { get; set; }
}