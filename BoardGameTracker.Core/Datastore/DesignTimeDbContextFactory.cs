using BoardGameTracker.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BoardGameTracker.Core.Datastore;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MainDbContext>
{
    public MainDbContext CreateDbContext(string[] args)
    {
        var dbConnectionProvider = new DbConnectionProvider();
        var connectionString = dbConnectionProvider.GetPostgresConnectionString(dbConnectionProvider.PostgresMainDb);

        var optionsBuilder = new DbContextOptionsBuilder<MainDbContext>();
        optionsBuilder.UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

        return new MainDbContext(optionsBuilder.Options);
    }
}
