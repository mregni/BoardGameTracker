using Ardalis.Specification;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Specifications;

public sealed class LinkedPlayerByUserIdSpec : Specification<ApplicationUser, DashboardLinkedPlayerDto>
{
    public LinkedPlayerByUserIdSpec(string userId)
    {
        Query
            .Where(x => x.Id == userId && x.PlayerId != null && x.Player != null)
            .AsNoTracking();

        Query.Select(x => new DashboardLinkedPlayerDto
        {
            Id = x.PlayerId!.Value,
            Name = x.Player!.Name
        });
    }
}
