using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Players;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IImageService _imageService;

    public PlayerService(IPlayerRepository playerRepository, IImageService imageService)
    {
        _playerRepository = playerRepository;
        _imageService = imageService;
    }

    public Task<List<Player>> GetPlayers()
    {
        return _playerRepository.GetPlayers();
    }

    public Task CreatePlayer(Player player)
    {
        return _playerRepository.CreatePlayer(player);
    }

    public Task<Player?> GetPlayer(int id)
    {
        return _playerRepository.GetPlayerById(id);
    }

    public async Task Delete(int id)
    {
        var player = await _playerRepository.GetPlayerById(id);
        if (player == null)
        {
            return;
        }
        
        _imageService.DeleteImage(player.Image);
        await _playerRepository.DeletePlayer(player);
    }

    public async Task<PlayerStatistics> GetStats(int id)
    {
        var stats =  new PlayerStatistics
        {
            PlayCount = await _playerRepository.GetPlayCount(id),
            BestGameId = await _playerRepository.GetBestGameId(id),
            FavoriteColor = await _playerRepository.GetFavoriteColor(id),
            WinCount = await _playerRepository.GetTotalWinCount(id),
            TotalPlayedTime = await _playerRepository.GetPlayLengthInMinutes(id)
        };
        
        return stats;
    }

    public Task<List<Play>> GetPlays(int id)
    {
        return _playerRepository.GetPlaysForPlayer(id);
    }
}