using Ardalis.GuardClauses;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.GameNights;

public class GameNightService : IGameNightService
{
    private readonly IGameNightRepository _gameNightRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GameNightService> _logger;

    public GameNightService(
        IGameNightRepository gameNightRepository,
        IUnitOfWork unitOfWork,
        IGameRepository gameRepository,
        ILogger<GameNightService> logger)
    {
        _gameNightRepository = gameNightRepository;
        _unitOfWork = unitOfWork;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public Task<List<GameNight>> GetGameNights()
    {
        _logger.LogDebug("Fetching game nights");
        return _gameNightRepository.GetAllAsync();
    }

    public Task<GameNight?> GetById(int id)
    {
        _logger.LogDebug("Fetching game night {GameNightId}", id);
        return _gameNightRepository.GetByIdAsync(id);
    }

    public async Task<GameNight> Create(CreateGameNightCommand command)
    {
        _logger.LogDebug("Creating game night {Title}", command.Title);
        var games = await _gameRepository.GetByIdsAsync(command.SuggestedGameIds);

        var players = command.InvitedPlayerIds.Select(playerId => GameNightRsvp.Create(playerId, GameNightRsvpState.Pending)).ToList();
        if (players.All(x => x.PlayerId != command.HostId))
        {
            players.Add(GameNightRsvp.Create(command.HostId, GameNightRsvpState.Accepted));
        }
        else
        {
            players.First(x => x.PlayerId == command.HostId).UpdateState(GameNightRsvpState.Accepted);
        }
        
        var gameNight = GameNight.Create(command.Title, command.Notes, command.StartDate, command.HostId, command.LocationId);
        
        gameNight.SetSuggestedGames(games);
        gameNight.SetInvitedPlayers(players);

        await _gameNightRepository.CreateAsync(gameNight);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Game night {GameNightId} ({Title}) created", gameNight.Id, gameNight.Title);

        return gameNight;
    }

    public async Task<GameNight> Update(UpdateGameNightCommand command)
    {
        _logger.LogDebug("Updating game night {GameNightId}", command.Id);
        var gameNight = await _gameNightRepository.GetByIdAsync(command.Id);
        if (gameNight == null)
        {
            throw new EntityNotFoundException(nameof(GameNight), command.Id);
        }

        var games = await _gameRepository.GetByIdsAsync(command.SuggestedGameIds);

        gameNight.Update(command.Title, command.Notes, command.StartDate, command.HostId, command.LocationId);
        gameNight.SetSuggestedGames(games);

        var existingPlayerIds = gameNight.InvitedPlayers.Select(p => p.PlayerId).ToHashSet();
        var newPlayerIds = command.InvitedPlayerIds.ToHashSet();

        var toRemove = gameNight.InvitedPlayers.Where(p => !newPlayerIds.Contains(p.PlayerId)).ToList();
        gameNight.RemoveInvitedPlayers(toRemove);

        var toAdd = newPlayerIds.Except(existingPlayerIds);
        gameNight.AddInvitedPlayers(toAdd);

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Game night {GameNightId} updated", command.Id);

        return gameNight;
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting game night {GameNightId}", id);
        await _gameNightRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Game night {GameNightId} deleted", id);
    }

    public async Task<GameNightRsvp> UpdateRsvp(UpdateRsvpCommand command)
    {
        GameNightRsvp? rsvp;
        if (command.Id.HasValue)
        {
            _logger.LogDebug("Updating RSVP {RsvpId}", command.Id);
            rsvp = await _gameNightRepository.GetRsvpByIdAsync(command.Id.Value);
        }
        else
        {
            Guard.Against.Null(command.GameNightId);
            Guard.Against.Null(command.PlayerId);
            _logger.LogDebug("Updating RSVP via rsvp page with gameNightId {GameNightId}, playerId: {PlayerId}", command.GameNightId, command.PlayerId);
            rsvp = await _gameNightRepository.GetRsvpByPlayerAndGameAsync(command.PlayerId.Value, command.GameNightId.Value);
        }
        
        Guard.Against.Null(rsvp);
        
        rsvp.UpdateState(command.State);
        await _unitOfWork.SaveChangesAsync();
        return rsvp;
    }

    public Task<int> CountFutureGameNights()
    {
        return _gameNightRepository.GetFutureGameNightsCountAsync();
    }

    public Task<GameNight?> GetByLinkId(Guid linkId)
    {
        return _gameNightRepository.GetGameNightByLinkId(linkId);
    }
}
