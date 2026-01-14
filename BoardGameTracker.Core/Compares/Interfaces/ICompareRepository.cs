using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface ICompareRepository
{
    Task<CompareRow<int>> GetDirectWins(int playerOne, int playerTwo);
    Task<CompareRow<MostWonGame?>> GetMostWonGame(int playerOne, int playerTwo);
    Task<int> GetTotalSessionsTogether(int playerOne, int playerTwo);
    Task<double> GetMinutesPlayedTogether(int playerOne, int playerTwo);
    Task<PreferredGame?> GetPreferredGame(int playerOne, int playerTwo);
    Task<LastWonGame?> GetLastWonGame(int playerOne, int playerTwo);
    Task<int?> GetLongestSessionTogether(int playerOne, int playerTwo);
    Task<FirstGameTogether?> GetFirstGameTogether(int playerOne, int playerTwo);
    Task<ClosestGame?> GetClosestGame(int playerOne, int playerTwo);
}