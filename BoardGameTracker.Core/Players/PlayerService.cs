using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Players;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IImageService _imageService;
    private readonly IPlayerStatisticsService _playerStatisticsService;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlayerService(
        IPlayerRepository playerRepository,
        IImageService imageService,
        IPlayerStatisticsService playerStatisticsService,
        IGameSessionRepository gameSessionRepository,
        ISessionRepository sessionRepository,
        IUnitOfWork unitOfWork)
    {
        _playerRepository = playerRepository;
        _imageService = imageService;
        _playerStatisticsService = playerStatisticsService;
        _gameSessionRepository = gameSessionRepository;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<List<Player>> GetList()
    {
        return _playerRepository.GetAllAsync();
    }

    public async Task<Player> Create(Player player)
    {
        await _playerRepository.CreateAsync(player);
        await _unitOfWork.SaveChangesAsync();
        return player;
    }

    public Task<Player?> Get(int id)
    {
        return _playerRepository.GetByIdAsync(id);
    }

    public async Task<Player?> Update(UpdatePlayerCommand command)
    {
        var dbPlayer = await _playerRepository.GetByIdAsync(command.Id);
        if (dbPlayer == null)
        {
            return null;
        }
        
        if (command.Image != dbPlayer.Image)
        {
            _imageService.DeleteImage(dbPlayer.Image);
            dbPlayer.UpdateImage(command.Image);
        }
        
        dbPlayer.UpdateName(command.Name);
        await _playerRepository.UpdateAsync(dbPlayer);
        await _unitOfWork.SaveChangesAsync();
        
        return dbPlayer;
    }

    public Task<int> CountAsync()
    {
        return _playerRepository.CountAsync();
    }

    public Task<List<Session>> GetSessions(int id, int? count)
    {
        return _gameSessionRepository.GetSessionsByPlayerId(id, count);
    }

    public async Task Delete(int id)
    {
        var player = await _playerRepository.GetByIdAsync(id);
        if (player == null)
        {
            return;
        }

        // Get all sessions where this player participated
        var sessions = await _sessionRepository.GetByPlayer(id);

        // Delete each session (this will cascade delete PlayerSessions and related data)
        foreach (var session in sessions)
        {
            await _sessionRepository.DeleteAsync(session.Id);
        }

        _imageService.DeleteImage(player.Image);
        await _playerRepository.DeleteAsync(player.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    public Task<PlayerStatistics> GetStats(int id)
    {
        return _playerStatisticsService.CalculateStatisticsAsync(id);
    }
    
    public Task<int> GetTotalPlayCount(int id)
    {
        return _playerRepository.GetTotalPlayCount(id);
    }
}