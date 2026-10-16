using BoardGameTracker.Common.Models.Compare;

namespace BoardGameTracker.Core.Compares.Interfaces;

public interface ICompareRepository
{
    Task<List<SessionTogether>> GetSessionsTogether(int playerOne, int playerTwo);
    Task<ComparePairDto?> GetPairWithMostSessions();
}
