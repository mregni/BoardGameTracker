using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Disk;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameTracker.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IDiskProvider, DiskProvider>();
        
        serviceCollection.AddTransient<IConfigFileProvider, ConfigFileProvider>();
        
        serviceCollection.AddTransient<IBggService, BggService>();
        serviceCollection.AddTransient<IGameService, GameService>();
        
        serviceCollection.AddTransient<IGameRepository, GameRepository>();

        serviceCollection.AddDbContext<MainContext>((serviceProvider, options) =>
        {
            var fileConfigProvider = serviceProvider.GetService<IConfigFileProvider>();
            var connectionString = fileConfigProvider.GetPostgresConnectionString(fileConfigProvider.PostgresMainDb);
            options.UseNpgsql(connectionString);
        });

        return serviceCollection;
    }
}