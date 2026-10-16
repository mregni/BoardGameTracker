using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IShameService
{
    Task<int> CountShelfOfShameGames(CancellationToken cancellationToken = default);
    Task<List<ShameGame>> GetShameGames(int? months = null);
    Task<ShameStatistics> GetShameStatistics(int? months = null);
}
