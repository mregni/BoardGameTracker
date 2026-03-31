using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Language : HasId
{
    public required string Key { get; set; }
    public required string TranslationKey { get; set; }
}