using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BoardGameTracker.Core.Datastore;

public static class MainDbContextOptions
{
    public static DbContextOptionsBuilder Configure(
        DbContextOptionsBuilder options,
        NpgsqlDataSource dataSource,
        IEnvironmentProvider environmentProvider)
    {
        return options
            .EnableSensitiveDataLogging(environmentProvider.EfSensitiveDataLogging)
            .UseNpgsql(dataSource, o => o.UseVector().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
}
