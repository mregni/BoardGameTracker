using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Config: HasId
{
    private string _key = null!;

    public required string Key
    {
        get => _key;
        set => _key = value.ToLowerInvariant();
    }
    public required string Value { get; set; }
}