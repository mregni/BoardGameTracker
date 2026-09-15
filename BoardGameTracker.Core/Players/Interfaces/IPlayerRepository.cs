using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerRepository: IRepository<Player>
{
    Task<List<MostPlayedGame>> GetMostPlayedGames(int playerId, int count);
    Task<double> GetPlayLengthInMinutes(int id);
    Task<int> GetDistinctGameCount(int id);
    Task<int> GetTotalPlayCount(int id);
    Task<int> GetTotalWinCount(int id);
    Task<List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>> GetTopPlayers(int count, CancellationToken cancellationToken = default);
    Task<List<LeaderboardRow>> GetLeaderboardRows(CancellationToken cancellationToken = default);
}