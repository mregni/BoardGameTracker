using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface ICompareService
{
    Task<CompareResult> GetPlayerComparisation(int playerOne, int playerTwo);
}