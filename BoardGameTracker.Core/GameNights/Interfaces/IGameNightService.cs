using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Interfaces;

public interface IGameNightService
{
    Task<List<GameNight>> GetGameNights(bool past);
    Task<GameNight?> GetById(int id);
    Task<GameNight> Create(CreateGameNightCommand command);
    Task<GameNight> Update(UpdateGameNightCommand command);
    Task Delete(int id);
    Task<GameNightRsvp> UpdateRsvp(UpdateRsvpCommand command);
    Task<int> CountFutureGameNights();
}
