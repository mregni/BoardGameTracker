using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Compares.DomainServices;

public interface IPlayerComparisonDomainService
{
    Task<PlayerComparison> ComparePlayersAsync(int playerOneId, int playerTwoId);
}
