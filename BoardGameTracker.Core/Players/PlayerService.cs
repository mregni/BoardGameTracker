using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Players;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IImageService _imageService;
    private readonly IPlayerStatisticsService _playerStatisticsService;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(
        IPlayerRepository playerRepository,
        IImageService imageService,
        IPlayerStatisticsService playerStatisticsService,
        IGameSessionRepository gameSessionRepository,
        ISessionRepository sessionRepository,
        IUnitOfWork unitOfWork,
        ILogger<PlayerService> logger)
    {
        _playerRepository = playerRepository;
        _imageService = imageService;
        _playerStatisticsService = playerStatisticsService;
        _gameSessionRepository = gameSessionRepository;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<Player>> GetList()
    {
        _logger.LogDebug("Fetching all players");
        return _playerRepository.GetAllAsync();
    }

    public async Task<Player> Create(CreatePlayerCommand command)
    {
        _logger.LogDebug("Creating player {Name}", command.Name);
        var player = new Player(command.Name, command.Image);
        await _playerRepository.CreateAsync(player);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Player {PlayerId} ({Name}) created", player.Id, player.Name);
        return player;
    }

    public Task<Player?> Get(int id)
    {
        _logger.LogDebug("Fetching player {PlayerId}", id);
        return _playerRepository.GetByIdAsync(id);
    }

    public async Task<Player> Update(UpdatePlayerCommand command)
    {
        _logger.LogDebug("Updating player {PlayerId}", command.Id);
        var dbPlayer = await _playerRepository.GetByIdAsync(command.Id);
        if (dbPlayer == null)
        {
            throw new EntityNotFoundException(nameof(Player), command.Id);
        }
        
        if (command.Image != dbPlayer.Image)
        {
            _imageService.DeleteImage(dbPlayer.Image);
            dbPlayer.UpdateImage(command.Image);
        }
        
        dbPlayer.UpdateName(command.Name);
        await _unitOfWork.SaveChangesAsync();
        
        return dbPlayer;
    }

    public Task<int> CountAsync()
    {
        return _playerRepository.CountAsync();
    }

    public Task<List<Session>> GetSessions(int id, int? count)
    {
        _logger.LogDebug("Fetching sessions for player {PlayerId}", id);
        return _gameSessionRepository.GetSessionsByPlayerId(id, count);
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting player {PlayerId}", id);
        var player = await _playerRepository.GetByIdAsync(id);
        if (player == null)
        {
            throw new EntityNotFoundException(nameof(Player), id);
        }

        await _sessionRepository.DeleteByPlayerIdAsync(id);

        _imageService.DeleteImage(player.Image);
        await _playerRepository.DeleteAsync(player.Id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Player {PlayerId} deleted", id);
    }

    public Task<PlayerStatistics> GetStats(int id)
    {
        _logger.LogDebug("Fetching stats for player {PlayerId}", id);
        return _playerStatisticsService.CalculateStatisticsAsync(id);
    }

    public Task<int> GetTotalPlayCount(int id)
    {
        _logger.LogDebug("Fetching total play count for player {PlayerId}", id);
        return _playerRepository.GetTotalPlayCount(id);
    }
}