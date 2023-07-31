using BoardGameTracker.Common.Entities;
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
}