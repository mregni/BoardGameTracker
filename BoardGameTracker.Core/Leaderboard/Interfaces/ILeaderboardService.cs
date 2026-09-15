using BoardGameTracker.Common.DTOs;

namespace BoardGameTracker.Core.Leaderboard.Interfaces;

public interface ILeaderboardService
{
    Task<LeaderboardDto> GetLeaderboardAsync(CancellationToken cancellationToken = default);
}
