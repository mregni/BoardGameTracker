using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Leaderboard.Interfaces;

public interface ILeaderboardService
{
    Task<LeaderboardDto> GetLeaderboardAsync(LeaderboardFilter filter, CancellationToken cancellationToken = default);
}
