using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface ICompareService
{
    Task<CompareResultDto> GetPlayerComparisation(int playerOne, int playerTwo);
}