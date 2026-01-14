using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Bgg.AntiCorruption;

public interface IBggGameTranslator
{
    Task<GameImportData> TranslateFromBggAsync(BggGame bggGame);
    BggGame TranslateRawGame(BggRawGame rawGame);
}
