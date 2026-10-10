using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class PlayerWithNameSpec : Specification<Player>
{
    public PlayerWithNameSpec(string name, int? excludedId = null)
    {
        var pattern = name.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        Query
            .Where(x => EF.Functions.ILike(x.Name.Trim(), pattern, "\\"))
            .Where(x => excludedId == null || x.Id != excludedId)
            .AsNoTracking();
    }
}
