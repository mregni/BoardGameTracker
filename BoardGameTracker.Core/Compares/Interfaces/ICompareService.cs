using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface ICompareService
{
    Task<CompareResultDto> GetPlayerComparison(int playerOne, int playerTwo);
}