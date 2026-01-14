using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Sessions.DomainServices;

public class FirstPlayDetectionService : IFirstPlayDetectionService
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public FirstPlayDetectionService(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<bool> IsFirstPlayAsync(int playerId, int gameId)
    {
        var sessions = await _gameSessionRepository.GetSessionsByPlayerId(playerId, null);
        return !sessions.Any(s => s.GameId == gameId);
    }

    public Task<bool> IsFirstPlayAsync(Player player, Game game)
    {
        return IsFirstPlayAsync(player.Id, game.Id);
    }

    public async Task<IEnumerable<int>> GetFirstTimePlayerIdsAsync(int gameId, IEnumerable<int> playerIds)
    {
        var firstTimePlayers = new List<int>();

        foreach (var playerId in playerIds)
        {
            var isFirstPlay = await IsFirstPlayAsync(playerId, gameId);
            if (isFirstPlay)
            {
                firstTimePlayers.Add(playerId);
            }
        }

        return firstTimePlayers;
    }
}
