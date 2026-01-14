using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Sessions;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IBadgeService _badgeService;
    private readonly IGameService _gameService;
    private readonly ILocationService _locationService;
    private readonly IUnitOfWork _unitOfWork;

    public SessionService(ISessionRepository sessionRepository, IBadgeService badgeService, IGameService gameService, ILocationService locationService, IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _badgeService = badgeService;
        _gameService = gameService;
        _locationService = locationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Session> Create(Session session)
    {
        session = await _sessionRepository.CreateAsync(session);
        await _badgeService.AwardBadgesAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return session;
    }

    public async Task<Session> CreateFromDto(SessionDto dto)
    {
        var session = dto.ToEntity();

        if (dto.Expansions.Count > 0)
        {
            var expansionIds = dto.Expansions.Select(e => e.Id).ToList();
            var expansions = await _gameService.GetGameExpansions(expansionIds);
            foreach (var expansion in expansions)
            {
                session.AddExpansion(expansion);
            }
        }

        foreach (var psDto in dto.PlayerSessions)
        {
            session.AddPlayerSession(psDto.PlayerId, psDto.Score, false, psDto.Won);
        }

        if (dto.LocationId.HasValue)
        {
            var locations = await _locationService.GetLocations();
            var location = locations.FirstOrDefault(l => l.Id == dto.LocationId.Value);
            session.SetLocation(location);
        }

        return await Create(session);
    }

    public Task Delete(int id)
    {
        return _sessionRepository.DeleteAsync(id);
    }

    public async Task<Session> Update(Session session)
    {
        session = await _sessionRepository.UpdateAsync(session);
        await _badgeService.AwardBadgesAsync(session);

        return session;
    }

    public async Task<Session> UpdateFromDto(SessionDto dto)
    {
        var existingSession = await _sessionRepository.GetByIdAsync(dto.Id);
        if (existingSession == null)
        {
            throw new InvalidOperationException($"Session with ID {dto.Id} not found");
        }

        existingSession.UpdateTimes(dto.Start, dto.End);
        existingSession.UpdateComment(dto.Comment);

        var newExpansionIds = dto.Expansions.Select(e => e.Id).ToList();
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

        var newPlayerIds = dto.PlayerSessions.Select(ps => ps.PlayerId).ToList();
        var currentPlayerIds = existingSession.PlayerSessions.Select(ps => ps.PlayerId).ToList();

        var playerIdsToRemove = currentPlayerIds.Except(newPlayerIds).ToList();
        foreach (var playerId in playerIdsToRemove)
        {
            existingSession.RemovePlayerSession(playerId);
        }

        foreach (var psDto in dto.PlayerSessions)
        {
            var existingPlayerSession = existingSession.PlayerSessions
                .FirstOrDefault(ps => ps.PlayerId == psDto.PlayerId);

            if (existingPlayerSession != null)
            {
                existingPlayerSession.UpdateScore(psDto.Score);
                if (psDto.Won)
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
                existingSession.AddPlayerSession(psDto.PlayerId, psDto.Score, false, psDto.Won);
            }
        }

        if (dto.LocationId.HasValue)
        {
            var locations = await _locationService.GetLocations();
            var location = locations.FirstOrDefault(l => l.Id == dto.LocationId.Value);
            existingSession.SetLocation(location);
        }
        else
        {
            existingSession.SetLocation(null);
        }

        return await Update(existingSession);
    }

    public async Task<Session> CreateFromCommand(CreateSessionCommand command)
    {
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
            var locations = await _locationService.GetLocations();
            var location = locations.FirstOrDefault(l => l.Id == command.LocationId.Value);
            session.SetLocation(location);
        }

        return await Create(session);
    }

    public async Task<Session> UpdateFromCommand(UpdateSessionCommand command)
    {
        var existingSession = await _sessionRepository.GetByIdAsync(command.Id);
        if (existingSession == null)
        {
            throw new InvalidOperationException($"Session with ID {command.Id} not found");
        }

        var end = command.Start.AddMinutes(command.Minutes);
        existingSession.UpdateTimes(command.Start, end);
        existingSession.UpdateComment(command.Comment ?? string.Empty);

        var newExpansionIds = command.ExpansionIds;
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

        var newPlayerIds = command.PlayerSessions.Select(ps => ps.PlayerId).ToList();
        var currentPlayerIds = existingSession.PlayerSessions.Select(ps => ps.PlayerId).ToList();

        var playerIdsToRemove = currentPlayerIds.Except(newPlayerIds).ToList();
        foreach (var playerId in playerIdsToRemove)
        {
            existingSession.RemovePlayerSession(playerId);
        }

        foreach (var psCommand in command.PlayerSessions)
        {
            var existingPlayerSession = existingSession.PlayerSessions
                .FirstOrDefault(ps => ps.PlayerId == psCommand.PlayerId);

            if (existingPlayerSession != null)
            {
                existingPlayerSession.UpdateScore(psCommand.Score);
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

        if (command.LocationId.HasValue)
        {
            var locations = await _locationService.GetLocations();
            var location = locations.FirstOrDefault(l => l.Id == command.LocationId.Value);
            existingSession.SetLocation(location);
        }
        else
        {
            existingSession.SetLocation(null);
        }

        return await Update(existingSession);
    }

    public Task<Session?> Get(int id)
    {
        return _sessionRepository.GetByIdAsync(id);
    }
}