namespace BoardGameTracker.Common.ViewModels.Language;

public class LanguageViewModel
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string TranslationKey { get; set; }
}