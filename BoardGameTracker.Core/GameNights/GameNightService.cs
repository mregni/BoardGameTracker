using Ardalis.GuardClauses;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.GameNights;

public class GameNightService : IGameNightService
{
    private readonly IGameNightRepository _gameNightRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MainDbContext _context;

    public GameNightService(
        IGameNightRepository gameNightRepository,
        IUnitOfWork unitOfWork,
        MainDbContext context)
    {
        _gameNightRepository = gameNightRepository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public Task<List<GameNight>> GetGameNights(bool past)
    {
        return past
            ? _gameNightRepository.GetPastGameNightsAsync()
            : _gameNightRepository.GetFutureGameNightsAsync();
    }

    public Task<GameNight?> GetById(int id)
    {
        return _gameNightRepository.GetByIdAsync(id);
    }

    public async Task<GameNight> Create(CreateGameNightCommand command)
    {
        var games = await _context.Games
            .Where(g => command.SuggestedGameIds.Contains(g.Id))
            .ToListAsync();

        var players = command.InvitedPlayerIds.Select(playerId => GameNightRsvp.Create(playerId, GameNightRsvpState.Pending)).ToList();
        players.Add(GameNightRsvp.Create(command.HostId, GameNightRsvpState.Accepted));
        var gameNight = GameNight.Create(command.Title, command.Notes, command.StartDate, command.HostId, command.LocationId);
        
        gameNight.SetSuggestedGames(games);
        gameNight.SetInvitedPlayers(players);

        await _gameNightRepository.CreateAsync(gameNight);
        await _unitOfWork.SaveChangesAsync();

        return (await _gameNightRepository.GetByIdAsync(gameNight.Id))!;
    }

    public async Task<GameNight> Update(UpdateGameNightCommand command)
    {
        var gameNight = await _gameNightRepository.GetByIdAsync(command.Id);
        Guard.Against.Null(gameNight);

        var games = await _context.Games
            .Where(g => command.SuggestedGameIds.Contains(g.Id))
            .ToListAsync();

        gameNight.Update(command.Title, command.Notes, command.StartDate, command.HostId, command.LocationId);
        gameNight.SetSuggestedGames(games);

        var existingPlayerIds = gameNight.InvitedPlayers.Select(p => p.PlayerId).ToHashSet();
        var newPlayerIds = command.InvitedPlayerIds.ToHashSet();

        var toRemove = gameNight.InvitedPlayers.Where(p => !newPlayerIds.Contains(p.PlayerId)).ToList();
        gameNight.RemoveInvitedPlayers(toRemove);

        var toAdd = newPlayerIds.Except(existingPlayerIds);
        gameNight.AddInvitedPlayers(toAdd);

        await _gameNightRepository.UpdateAsync(gameNight);
        await _unitOfWork.SaveChangesAsync();

        return (await _gameNightRepository.GetByIdAsync(gameNight.Id))!;
    }

    public async Task Delete(int id)
    {
        await _gameNightRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<GameNightRsvp> UpdateRsvp(UpdateRsvpCommand command)
    {
        var rsvp = await _gameNightRepository.GetRsvpByIdAsync(command.Id);
        if (rsvp == null)
        {
            throw new KeyNotFoundException($"RSVP with id {command.Id} not found.");
        }

        rsvp.UpdateState(command.State);
        await _gameNightRepository.UpdateRsvpAsync(rsvp);
        await _unitOfWork.SaveChangesAsync();

        return rsvp;
    }

    public Task<int> CountFutureGameNights()
    {
        return _gameNightRepository.GetFutureGameNightsCountAsync();
    }
}
