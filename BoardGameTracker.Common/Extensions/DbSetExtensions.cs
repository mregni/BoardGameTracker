using BoardGameTracker.Common.Entities.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Common.Extensions;

public static class DbSetExtensions
{
    public static async Task AddRangeIfNotExists<T>(this DbSet<T> dbSet, IEnumerable<T> list) where T : HasId
    {
        foreach (var item in list)
        {
            if (! await dbSet.AnyAsync(x => x.Id == item.Id))
            {
                await dbSet.AddAsync(item);
            }
        }
    }
}