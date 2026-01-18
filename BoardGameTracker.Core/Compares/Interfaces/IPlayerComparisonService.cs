using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface IPlayerComparisonService
{
    Task<PlayerComparison> ComparePlayersAsync(int playerOneId, int playerTwoId);
}
