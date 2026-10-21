using Ardalis.Specification;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Specifications;

public sealed class LinkedAccountByPlayerIdSpec : Specification<ApplicationUser, PlayerLinkedAccountDto>
{
    public LinkedAccountByPlayerIdSpec(int playerId)
    {
        Query
            .Where(x => x.PlayerId == playerId)
            .AsNoTracking();

        Query.Select(x => new PlayerLinkedAccountDto
        {
            Id = x.Id,
            Username = x.UserName ?? string.Empty,
            DisplayName = x.DisplayName,
            ProfileImage = x.ProfileImage
        });
    }
}
