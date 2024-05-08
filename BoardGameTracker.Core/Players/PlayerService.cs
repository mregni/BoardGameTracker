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

    public Task<Player> Create(Player player)
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

    public async Task<Player> Update(Player player)
    {
        var dbPlayer = await _playerRepository.GetById(player.Id);
        if (dbPlayer != null && player.Image != dbPlayer.Image)
        {
            _imageService.DeleteImage(dbPlayer.Image);
        }

        return await _playerRepository.Update(player);
    }

    public Task<int> CountAsync()
    {
        return _playerRepository.CountAsync();
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
            WinCount = await _playerRepository.GetTotalWinCount(id),
            TotalPlayedTime = await _playerRepository.GetPlayLengthInMinutes(id),
            DistinctGameCount = await _playerRepository.GetDistinctGameCount(id)
        };

        return stats;
    }

    public Task<List<Play>> GetPlays(int id, int skip, int? take)
    {
        return _playerRepository.GetPlaysForPlayer(id, skip, take);
    }
    
    public Task<int> GetTotalPlayCount(int id)
    {
        return _playerRepository.GetTotalPlayCount(id);
    }
}