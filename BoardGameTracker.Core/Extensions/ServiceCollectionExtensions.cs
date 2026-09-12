using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Compares;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Dashboard;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Email;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Languages;
using BoardGameTracker.Core.Languages.Interfaces;
using BoardGameTracker.Core.Loans;
using BoardGameTracker.Core.Loans.Interfaces;
using BoardGameTracker.Core.Locations;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Manuals;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Maintenance;
using BoardGameTracker.Core.Maintenance.Interfaces;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.GameNights;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.Settings;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Core.Updates.Interfaces;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Pgvector.Npgsql;

namespace BoardGameTracker.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IDiskProvider, DiskProvider>();

        serviceCollection.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        serviceCollection.AddSingleton<IDbConnectionProvider, DbConnectionProvider>();
        serviceCollection.AddScoped<IConfigRepository, ConfigRepository>();
        serviceCollection.AddSingleton<IEnvironmentProvider, EnvironmentProvider>();
        
        serviceCollection.AddScoped<IGameService, GameService>();
        serviceCollection.AddScoped<IChangeDetectionClient, ChangeDetectionClient>();
        serviceCollection.AddScoped<IBggImportService, BggImportService>();
        serviceCollection.AddScoped<IGameChartService, GameChartService>();
        serviceCollection.AddScoped<IShameService, ShameService>();
        serviceCollection.AddScoped<IImageService, ImageService>();
        serviceCollection.AddScoped<IManualService, ManualService>();

        serviceCollection.AddSingleton<IManualIndexingQueue, ManualIndexingQueue>();
        serviceCollection.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
        serviceCollection.AddSingleton<IPdfPageRenderer, PdfPageRenderer>();
        serviceCollection.AddSingleton<IRulebookChunker, RulebookChunker>();
        serviceCollection.AddScoped<IRagSettingsProvider, RagSettingsProvider>();
        serviceCollection.AddScoped<IAiClientFactory, AiClientFactory>();
        serviceCollection.AddScoped<IManualChunkRepository, ManualChunkRepository>();
        serviceCollection.AddScoped<IManualIndexingService, ManualIndexingService>();
        serviceCollection.AddScoped<IRagService, RagService>();

        if (bool.TryParse(Environment.GetEnvironmentVariable("RAG_ENABLED"), out var ragEnabled) && ragEnabled)
        {
            serviceCollection.AddHostedService<ModelProvisioningBackgroundService>();
            serviceCollection.AddHostedService<ManualIndexingBackgroundService>();
        }
        serviceCollection.AddScoped<IEmailService, EmailService>();
        serviceCollection.AddScoped<ISmtpSender, MailKitSmtpSender>();
        serviceCollection.AddScoped<IPublicUrlBuilder, PublicUrlBuilder>();
        serviceCollection.AddScoped<IPlayerService, PlayerService>();
        serviceCollection.AddScoped<ISessionService, SessionService>();
        serviceCollection.AddScoped<ILocationService, LocationService>();
        serviceCollection.AddScoped<ILoanService, LoanService>();
        serviceCollection.AddScoped<IDashboardService, DashboardService>();
        serviceCollection.AddScoped<ILanguageService, LanguageService>();
        serviceCollection.AddScoped<IBadgeService, BadgeService>();
        serviceCollection.AddScoped<ICompareService, CompareService>();
        serviceCollection.AddScoped<IUpdateService, UpdateService>();
        serviceCollection.AddScoped<IGameNightService, GameNightService>();
        serviceCollection.AddScoped<ISettingsService, SettingsService>();
        serviceCollection.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
        serviceCollection.AddScoped<IMaintenanceSeeder, MaintenanceSeeder>();
        serviceCollection.AddScoped<IResetService, ResetService>();

        serviceCollection.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        serviceCollection.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));

        serviceCollection.AddScoped<IGameRepository, GameRepository>();
        serviceCollection.AddScoped<IGameStatisticsRepository, GameStatisticsRepository>();
        serviceCollection.AddScoped<IPlayerRepository, PlayerRepository>();
        serviceCollection.AddScoped<ISessionRepository, SessionRepository>();
        serviceCollection.AddScoped<IBadgeRepository, BadgeRepository>();
        serviceCollection.AddScoped<ICompareRepository, CompareRepository>();

        serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
        serviceCollection.AddScoped<ITokenService, TokenService>();
        serviceCollection.AddScoped<IOidcService, OidcService>();
        serviceCollection.AddScoped<IAuthService, AuthService>();
        serviceCollection.AddScoped<IOidcProviderService, OidcProviderService>();
        serviceCollection.AddScoped<IUserAdminService, UserAdminService>();
        serviceCollection.AddSingleton<IHostedService, RefreshTokenCleanupService>();

        serviceCollection.AddScoped<IGameStatisticsService, GameStatisticsService>();
        serviceCollection.AddScoped<IPlayerStatisticsService, PlayerStatisticsService>();
        serviceCollection.AddScoped<IBadgeProgressionService, BadgeProgressionService>();

        serviceCollection.AddScoped<IGameFactory, GameFactory>();

        serviceCollection.AddScoped<IBadgeLevelProgressionPolicy, BadgeLevelProgressionPolicy>();

        serviceCollection.AddScoped<IBadgeEvaluator, SessionsBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, DifferentGameBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, SessionWinEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, DurationBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, WinPercentageBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, SoloSpecialistBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, CloseWinBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, CloseLossBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, MarathonRunnerBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, FirstTryBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, LearningCurveBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, MonthlyGoalBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, ConsistentScheduleBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, SocialPlayerBadgeEvaluator>();
        serviceCollection.AddScoped<IBadgeEvaluator, WinningStreakBadgeEvaluator>();
        
        serviceCollection.AddSingleton(serviceProvider =>
        {
            var dbConnectionProvider = serviceProvider.GetService<IDbConnectionProvider>();
            if (dbConnectionProvider == null)
            {
                throw new ServiceNotResolvedException("dbConnectionProvider could not be resolved");
            }

            var connectionString = dbConnectionProvider.GetPostgresConnectionString(dbConnectionProvider.PostgresMainDb);
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();
            return dataSourceBuilder.Build();
        });

        serviceCollection.AddDbContext<MainDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetService<NpgsqlDataSource>();
            if (dataSource == null)
            {
                throw new ServiceNotResolvedException("NpgsqlDataSource could not be resolved");
            }

            var environmentProvider = serviceProvider.GetService<IEnvironmentProvider>();
            if (environmentProvider == null)
            {
                throw new ServiceNotResolvedException("environmentProvider could not be resolved");
            }

            options
                .EnableSensitiveDataLogging(environmentProvider.IsDevelopment)
                .UseNpgsql(dataSource, o => o.UseVector().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        return serviceCollection;
    }
}