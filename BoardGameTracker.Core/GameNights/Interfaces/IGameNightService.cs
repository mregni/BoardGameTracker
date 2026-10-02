using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Interfaces;

public interface IGameNightService
{
    Task<SendInvitesResultDto> SendInvitesAsync(int id, CancellationToken cancellationToken = default);
    Task<List<GameNight>> GetGameNights();
    Task<GameNight?> GetById(int id);
    Task<GameNight> Create(CreateGameNightCommand command);
    Task<GameNight> Update(UpdateGameNightCommand command);
    Task Delete(int id);
    Task<GameNightRsvp> UpdateRsvp(UpdateRsvpCommand command);
    Task<GameNightRsvp> UpdateRsvpByLink(Guid linkId, UpdateRsvpCommand command, bool isAuthenticated);
    Task<int> CountFutureGameNights(CancellationToken cancellationToken = default);
    Task<GameNight?> GetByLinkId(Guid linkId, bool isAuthenticated);
}
