using Ardalis.Specification;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Auth.Specifications;

public sealed class AccountsAwaitingPlayerImageSpec : Specification<ApplicationUser>
{
    public AccountsAwaitingPlayerImageSpec()
    {
        Query
            .Where(x => x.PlayerId != null
                        && x.ProfileImage != null
                        && x.Player != null
                        && x.Player.ImageSource == PlayerImageSource.None)
            .AsNoTracking();
    }
}
