using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface ICompareRepository
{
    Task<CompareRow<int>> GetDirectWins(int playerOne, int playerTwo);
    Task<CompareRow<MostWonGame?>> GetMostWonGame(int playerOne, int playerTwo);
    
}