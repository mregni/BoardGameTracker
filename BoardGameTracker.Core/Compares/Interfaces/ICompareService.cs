using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface ICompareService
{
    Task<CompareResultDto> GetPlayerComparison(int playerOne, int playerTwo);
    Task<ComparePairDto?> GetDefaultPair();
}
