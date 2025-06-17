using BoardGameTracker.Common.Exeptions;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Dashboard;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Disk;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Languages;
using BoardGameTracker.Core.Languages.Interfaces;
using BoardGameTracker.Core.Locations;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameTracker.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IDiskProvider, DiskProvider>();
        
        serviceCollection.AddScoped<IConfigFileProvider, ConfigFileProvider>();
        serviceCollection.AddScoped<IEnvironmentProvider, EnvironmentProvider>();
        
        serviceCollection.AddScoped<IGameService, GameService>();
        serviceCollection.AddScoped<IImageService, ImageService>();
        serviceCollection.AddScoped<IPlayerService, PlayerService>();
        serviceCollection.AddScoped<ISessionService, SessionService>();
        serviceCollection.AddScoped<ILocationService, LocationService>();
        serviceCollection.AddScoped<IDashboardService, DashboardService>();
        serviceCollection.AddScoped<ILanguageService, LanguageService>();
        serviceCollection.AddScoped<IBadgeService, BadgeService>();
        
        serviceCollection.AddScoped<IGameRepository, GameRepository>();
        serviceCollection.AddScoped<IPlayerRepository, PlayerRepository>();
        serviceCollection.AddScoped<ISessionRepository, SessionRepository>();
        serviceCollection.AddScoped<ILocationRepository, LocationRepository>();
        serviceCollection.AddScoped<IDashboardRepository, DashboardRepository>();
        serviceCollection.AddScoped<ILanguageRepository, LanguageRepository>();
        serviceCollection.AddScoped<IBadgeRepository, BadgeRepository>();
        
        serviceCollection.AddScoped<IBadgeEvaluator, SessionsBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, DifferentGameBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, SessionWinEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, DurationBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, WinPercentageBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, SoloSpecialistBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, CloseWinBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, CloseLossBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, MarathonRunnerBadgeEvaluator>();

        serviceCollection.AddDbContext<MainDbContext>((serviceProvider, options) =>
        {
            var fileConfigProvider = serviceProvider.GetService<IConfigFileProvider>();
            if (fileConfigProvider == null)
            {
                throw new ServiceNotResolvedException("fileConfigProvider could not be resolved");
            }
            
            var environmentProvider = serviceProvider.GetService<IEnvironmentProvider>();
            if (environmentProvider == null)
            {
                throw new ServiceNotResolvedException("environmentProvider could not be resolved");
            }
            
            var connectionString = fileConfigProvider.GetPostgresConnectionString(fileConfigProvider.PostgresMainDb);
            options
                .EnableSensitiveDataLogging(environmentProvider.IsDevelopment)
                .UseNpgsql(connectionString);
        });

        return serviceCollection;
    }
}