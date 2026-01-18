using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Bgg.Interfaces;

public interface IBggGameTranslator
{
    Task<GameImportData> TranslateFromBggAsync(BggGame bggGame);
    BggGame TranslateRawGame(BggRawGame rawGame);
}
