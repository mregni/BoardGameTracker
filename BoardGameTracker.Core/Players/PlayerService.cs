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
        return _playerRepository.GetAllAsync();
    }

    public Task<Player> Create(Player player)
    {
        return _playerRepository.CreateAsync(player);
    }

    public Task<Player?> Get(int id)
    {
        return _playerRepository.GetByIdAsync(id);
    }

    public async Task<Player> Update(Player player)
    {
        var dbPlayer = await _playerRepository.GetByIdAsync(player.Id);
        if (dbPlayer != null && player.Image != dbPlayer.Image)
        {
            _imageService.DeleteImage(dbPlayer.Image);
            
            dbPlayer.Image = player.Image;
            dbPlayer.Name = player.Name;
            return await _playerRepository.UpdateAsync(dbPlayer);
        }

        return player;
    }

    public Task<int> CountAsync()
    {
        return _playerRepository.CountAsync();
    }

    public Task<List<Session>> GetSessions(int id)
    {
        return _playerRepository.GetSessions(id);
    }

    public async Task Delete(int id)
    {
        var player = await _playerRepository.GetByIdAsync(id);
        if (player == null)
        {
            return;
        }

        _imageService.DeleteImage(player.Image);
        await _playerRepository.DeleteAsync(player.Id);
    }

    public async Task<PlayerStatistics> GetStats(int id)
    {
        var stats = new PlayerStatistics
        {
            PlayCount = await _playerRepository.GetTotalPlayCount(id),
            WinCount = await _playerRepository.GetTotalWinCount(id),
            TotalPlayedTime = await _playerRepository.GetPlayLengthInMinutes(id),
            DistinctGameCount = await _playerRepository.GetDistinctGameCount(id)
        };

        var game = await _playerRepository.GetBestGame(id);
        if (game != null)
        {
            var wins = await _playerRepository.GetWinCount(id, game.Id);
            stats.MostWinsGame = new BestWinningGame
            {
                Id = game.Id,
                Image = game.Image,
                Title = game.Title,
                TotalWins = wins
            };
        }

        return stats;
    }
    
    public Task<int> GetTotalPlayCount(int id)
    {
        return _playerRepository.GetTotalPlayCount(id);
    }
}