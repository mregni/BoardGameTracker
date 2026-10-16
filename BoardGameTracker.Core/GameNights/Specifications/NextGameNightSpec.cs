using Ardalis.Specification;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class NextGameNightSpec : Specification<GameNight, DashboardGameNightDto>
{
    public NextGameNightSpec(DateTime now)
    {
        var playingSince = now - GameNight.PlayWindow;
        Query
            .Where(x => x.EndedAt == null && x.StartDate > playingSince)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.Id)
            .AsNoTracking();

        Query.Select(x => new DashboardGameNightDto
        {
            Id = x.Id,
            Title = x.Title,
            StartDate = x.StartDate,
            LocationName = x.Location.Name,
            HostId = x.HostId,
            HostName = x.Host.Name,
            AcceptedCount = x.InvitedPlayers.Count(r => r.State == GameNightRsvpState.Accepted),
            PendingCount = x.InvitedPlayers.Count(r => r.State == GameNightRsvpState.Pending),
            DeclinedCount = x.InvitedPlayers.Count(r => r.State == GameNightRsvpState.Declined),
            SuggestedGames = x.SuggestedGames
                .OrderBy(g => g.Title)
                .Select(g => new DashboardGameDto { Id = g.Id, Title = g.Title, Image = g.Image })
                .ToList()
        });
    }
}
