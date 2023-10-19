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

    public Task<List<Player>> GetList()
    {
        return _playerRepository.GetList();
    }

    public Task Create(Player player)
    {
        if (player.Image == null)
        {
            _imageService.SaveProfileImage(null);
        }

        return _playerRepository.Create(player);
    }

    public Task<Player?> Get(int id)
    {
        return _playerRepository.GetById(id);
    }

    public async Task Update(Player player)
    {
        var dbPlayer = await _playerRepository.GetById(player.Id);
        if (dbPlayer != null && player.Image != dbPlayer.Image)
        {
            _imageService.DeleteImage(dbPlayer.Image);
        }

        await _playerRepository.Update(player);
    }

    public async Task Delete(int id)
    {
        var player = await _playerRepository.GetById(id);
        if (player == null)
        {
            return;
        }

        _imageService.DeleteImage(player.Image);
        await _playerRepository.DeletePlayer(player);
    }

    public async Task<PlayerStatistics> GetStats(int id)
    {
        var stats = new PlayerStatistics
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