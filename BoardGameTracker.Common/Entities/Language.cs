using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Language : HasId
{
    public string Key { get; set; }
    public string TranslationKey { get; set; }
}