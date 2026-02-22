using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.GameNights.Interfaces;

public interface IGameNightRepository : ICrudHelper<GameNight>
{
    Task<List<GameNight>> GetFutureGameNightsAsync();
    Task<List<GameNight>> GetPastGameNightsAsync();
    Task<GameNightRsvp?> GetRsvpByIdAsync(int rsvpId);
    Task<GameNightRsvp> UpdateRsvpAsync(GameNightRsvp rsvp);
    Task<int> GetFutureGameNightsCountAsync();
    Task<GameNightRsvp?> GetRsvpByPlayerAndGameAsync(int commandPlayerId, int commandGameNightId);
    Task<GameNight?> GetGameNightByLinkId(Guid linkId);
}
