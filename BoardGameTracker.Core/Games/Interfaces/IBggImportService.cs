using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IBggImportService
{
    Task<Game?> ImportGameFromBgg(BggSearch search);
    Task<IList<BggImportGame>> ImportBggCollection(string userName);
    Task ImportList(IList<ImportGame> games);
}
