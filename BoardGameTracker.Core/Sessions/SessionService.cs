using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Sessions;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IBadgeService _badgeService;
    private readonly IGameService _gameService;
    private readonly ILocationService _locationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SessionService> _logger;

    public SessionService(ISessionRepository sessionRepository, IBadgeService badgeService, IGameService gameService, ILocationService locationService, IUnitOfWork unitOfWork, ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _badgeService = badgeService;
        _gameService = gameService;
        _locationService = locationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Session> Create(Session session)
    {
        _logger.LogDebug("Creating session for game {GameId}", session.GameId);
        session = await _sessionRepository.CreateAsync(session);
        await _badgeService.AwardBadgesAsync(session);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Session {SessionId} created for game {GameId}", session.Id, session.GameId);

        return session;
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting session {SessionId}", id);
        await _sessionRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Session {SessionId} deleted", id);
    }

    public async Task<Session> Update(Session session)
    {
        _logger.LogDebug("Updating session {SessionId}", session.Id);
        session = await _sessionRepository.Update(session);
        await _badgeService.AwardBadgesAsync(session);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Session {SessionId} updated", session.Id);

        return session;
    }

    public async Task<Session> CreateFromCommand(CreateSessionCommand command)
    {
        _logger.LogDebug("Creating session from command for game {GameId}", command.GameId);
        var end = command.Start.AddMinutes(command.Minutes);
        var session = new Session(command.GameId, command.Start, end, command.Comment ?? string.Empty);

        if (command.ExpansionIds.Count > 0)
        {
            var expansions = await _gameService.GetGameExpansions(command.ExpansionIds);
            foreach (var expansion in expansions)
            {
                session.AddExpansion(expansion);
            }
        }

        foreach (var psCommand in command.PlayerSessions)
        {
            session.AddPlayerSession(psCommand.PlayerId, psCommand.Score, psCommand.FirstPlay, psCommand.Won);
        }

        if (command.LocationId.HasValue)
        {
            var location = await _locationService.GetByIdAsync(command.LocationId.Value);
            session.SetLocation(location);
        }

        return await Create(session);
    }

    public async Task<Session> UpdateFromCommand(UpdateSessionCommand command)
    {
        _logger.LogDebug("Updating session {SessionId} from command", command.Id);
        var existingSession = await _sessionRepository.GetByIdAsync(command.Id);
        if (existingSession == null)
        {
            throw new EntityNotFoundException(nameof(Session), command.Id);
        }

        var end = command.Start.AddMinutes(command.Minutes);
        existingSession.UpdateTimes(command.Start, end);
        existingSession.UpdateComment(command.Comment ?? string.Empty);

        await SyncExpansions(existingSession, command.ExpansionIds);
        SyncPlayerSessions(existingSession, command.PlayerSessions);
        await UpdateLocation(existingSession, command.LocationId);

        return await Update(existingSession);
    }

    private async Task SyncExpansions(Session existingSession, List<int> newExpansionIds)
    {
        var currentExpansionIds = existingSession.Expansions.Select(e => e.Id).ToList();

        var expansionsToRemove = existingSession.Expansions
            .Where(e => !newExpansionIds.Contains(e.Id))
            .ToList();
        foreach (var expansion in expansionsToRemove)
        {
            existingSession.RemoveExpansion(expansion);
        }

        var expansionIdsToAdd = newExpansionIds.Except(currentExpansionIds).ToList();
        if (expansionIdsToAdd.Count > 0)
        {
            var expansionsToAdd = await _gameService.GetGameExpansions(expansionIdsToAdd);
            foreach (var expansion in expansionsToAdd)
            {
                existingSession.AddExpansion(expansion);
            }
        }
    }

    private static void SyncPlayerSessions(Session existingSession, List<CreatePlayerSessionCommand> playerSessionCommands)
    {
        var newPlayerIds = playerSessionCommands.Select(ps => ps.PlayerId).ToList();
        var currentPlayerIds = existingSession.PlayerSessions.Select(ps => ps.PlayerId).ToList();

        var playerIdsToRemove = currentPlayerIds.Except(newPlayerIds).ToList();
        foreach (var playerId in playerIdsToRemove)
        {
            existingSession.RemovePlayerSession(playerId);
        }

        foreach (var psCommand in playerSessionCommands)
        {
            var existingPlayerSession = existingSession.PlayerSessions
                .FirstOrDefault(ps => ps.PlayerId == psCommand.PlayerId);

            if (existingPlayerSession != null)
            {
                existingPlayerSession.UpdateScore(psCommand.Score);
                existingPlayerSession.UpdateFirstPlay(psCommand.FirstPlay);
                if (psCommand.Won)
                {
                    existingPlayerSession.MarkAsWinner();
                }
                else
                {
                    existingPlayerSession.MarkAsLoser();
                }
            }
            else
            {
                existingSession.AddPlayerSession(psCommand.PlayerId, psCommand.Score, psCommand.FirstPlay, psCommand.Won);
            }
        }
    }

    private async Task UpdateLocation(Session existingSession, int? locationId)
    {
        if (locationId.HasValue)
        {
            var location = await _locationService.GetByIdAsync(locationId.Value);
            existingSession.SetLocation(location);
        }
        else
        {
            existingSession.SetLocation(null);
        }
    }

    public Task<Session?> Get(int id)
    {
        _logger.LogDebug("Fetching session {SessionId}", id);
        return _sessionRepository.GetByIdAsync(id);
    }
}
