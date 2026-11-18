using Ardalis.GuardClauses;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.OpenApi;
using Npgsql;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;

namespace BoardGameTracker.Host.Startup;

internal static class StartupTasks
{
    public static async Task WriteOpenApiDocument(IServiceProvider services, string path)
    {
        var document = services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var writer = new StreamWriter(fullPath) { NewLine = "\n" };
        document.SerializeAsV3(new OpenApiJsonWriter(writer));
    }

    public static void CreateFolders(IServiceProvider serviceProvider)
    {
        var diskProvider = Guard.Against.Null(serviceProvider.GetService<IDiskProvider>());

        diskProvider.EnsureFolder(PathHelper.FullRootImagePath);
        diskProvider.EnsureFolder(PathHelper.FullCoverImagePath);
        diskProvider.EnsureFolder(PathHelper.FullProfileImagePath);
        diskProvider.EnsureFolder(PathHelper.FullManualsPath);
    }

    public static void LogStartupSummary(IServiceProvider services, IEnvironmentProvider environmentProvider, bool authEnabled)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation(
            "BoardGameTracker started\n  Environment:  {Environment}\n  Log level:    {LogLevel}\n  Sentry:       {SentryEnabled}\n  HTTP ports:   {HttpPorts}\n  Timezone:     {Timezone}\n  DB port:      {DbPort}\n  Auth:         {AuthState}",
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            LogLevelExtensions.GetEnvironmentLogLevel(),
            Environment.GetEnvironmentVariable("STATISTICS_ENABLED")?.ToLower() == "true" ? "Enabled" : "Disabled",
            Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "default",
            Environment.GetEnvironmentVariable("TZ") ?? "system default",
            Environment.GetEnvironmentVariable("DB_PORT") ?? "5432",
            authEnabled ? "Enabled" : "Disabled");
        if (!authEnabled)
        {
            logger.LogWarning(
                "AUTH_ENABLED=false: every request is treated as an administrator. Anyone who can reach this port can change settings, store API keys, upload files, import games and use the AI assistant. Only use this on a trusted network or behind a proxy that authenticates for you");
        }

        logger.WarnWhenKeysAreUnencrypted(services);

        if (environmentProvider.EfSensitiveDataLogging)
        {
            logger.LogWarning(
                "EF_SENSITIVE_LOGGING=true: database parameter values are written to the logs, including API keys, passwords, password hashes and tokens. Only use this for local debugging and remove it afterwards");
        }
    }

    public static async Task PrepareDataAsync(IServiceProvider services, bool authEnabled, string? adminPassword)
    {
        RunDbMigrations(services);
        await EncryptDataProtectionKeys(services);
        await SeedConfig(services);
        await ProtectIntegrationKeys(services);
        await BackfillPlayerAccountImages(services);
        if (authEnabled)
        {
            await SeedAuthData(services, adminPassword);
        }
    }

    private static void RunDbMigrations(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = Guard.Against.Null(scope.ServiceProvider.GetRequiredService<MainDbContext>());
        WaitForDatabase(context);
        EnsureVectorExtensionAvailable(context);
        context.Database.Migrate();
    }

    private static void WaitForDatabase(MainDbContext context)
    {
        var creator = context.Database.GetService<IRelationalDatabaseCreator>();
        const int attempts = 10;
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                if (!creator.Exists())
                {
                    Log.Information("Database {Database} does not exist yet; creating it", context.Database.GetDbConnection().Database);
                    creator.Create();
                }

                return;
            }
            catch (Exception ex) when (ex.GetBaseException() is NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
            {
                var reason = ex.GetBaseException().Message;
                if (attempt >= attempts)
                {
                    throw new InvalidOperationException(
                        $"Could not open the PostgreSQL database after {attempts} attempts ({reason}). Check DB_HOST, DB_PORT, DB_USER and DB_PASSWORD and make sure the database container is running.",
                        ex);
                }

                Log.Warning(ex, "Database not reachable yet (attempt {Attempt}/{Attempts}): {Message}", attempt, attempts, reason);
            }

            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }

    private static void EnsureVectorExtensionAvailable(MainDbContext context)
    {
        var available = context.Database
            .SqlQueryRaw<int>("SELECT 1 AS \"Value\" FROM pg_available_extensions WHERE name = 'vector'")
            .AsEnumerable()
            .Any();

        if (available)
        {
            return;
        }

        const string message =
            "The PostgreSQL server does not provide the 'vector' extension, which BoardGameTracker requires. " +
            "Use the pgvector/pgvector:pg16 image instead of postgres:16 (your existing data folder keeps working), " +
            "or install pgvector on your own server. See the Upgrading page in the documentation.";
        Log.Fatal(message);
        throw new InvalidOperationException(message);
    }

    private static async Task SeedConfig(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var configRepository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        await configRepository.SeedConfigAsync(ConfigDefaults.All);
    }

    private static async Task EncryptDataProtectionKeys(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var reEncryptor = scope.ServiceProvider.GetRequiredService<IDataProtectionKeyReEncryptor>();
        await reEncryptor.EncryptPlaintextKeysAsync();
    }

    private static async Task ProtectIntegrationKeys(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IIntegrationKeyMigrator>();
        await migrator.ProtectLegacyKeysAsync();
    }

    private static async Task BackfillPlayerAccountImages(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var accountImages = scope.ServiceProvider.GetRequiredService<IPlayerAccountImageService>();
        try
        {
            await accountImages.BackfillAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not give linked players the profile picture of their account");
        }
    }

    private static async Task SeedAuthData(IServiceProvider serviceProvider, string? adminPassword)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DbSeeder.SeedAuthData(roleManager, userManager, seedLogger, adminPassword);
    }
}
