using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(Infrastructure.IntegrationCollection.Name)]
public class PlayerImageSourceMigrationTests : IAsyncLifetime
{
    private const string BeforeImageSource = "20261021110355_AddUserProfileImage";

    private const string SeedPlayers = """
        INSERT INTO "Players" ("Name", "Image") VALUES
            ('With picture', '/images/profile/own.webp'),
            ('Empty picture', ''),
            ('Without picture', NULL);
        """;

    private readonly string _database = "player_image_source_" + Guid.NewGuid().ToString("N");
    private NpgsqlDataSource _dataSource = null!;

    public async ValueTask InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(ConnectionString("postgres")))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlDataSourceBuilder(ConnectionString(_database));
        builder.UseVector();
        _dataSource = builder.Build();
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(ConnectionString("postgres"));
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Upgrading_ShouldMarkExistingPlayerPicturesAsUploaded()
    {
        await using (var context = CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(BeforeImageSource, TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(SeedPlayers, TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            var players = await context.Players.AsNoTracking().ToDictionaryAsync(x => x.Name, x => x.ImageSource, TestContext.Current.CancellationToken);
            players["With picture"].Should().Be(PlayerImageSource.Upload);
            players["Empty picture"].Should().Be(PlayerImageSource.None);
            players["Without picture"].Should().Be(PlayerImageSource.None);
        }
    }

    private MainDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MainDbContext>().UseNpgsql(_dataSource, o => o.UseVector()).Options);

    private static string ConnectionString(string database) => new NpgsqlConnectionStringBuilder
    {
        Host = Environment.GetEnvironmentVariable("DB_HOST"),
        Port = int.Parse(Environment.GetEnvironmentVariable("DB_PORT")!, System.Globalization.CultureInfo.InvariantCulture),
        Username = Environment.GetEnvironmentVariable("DB_USER"),
        Password = Environment.GetEnvironmentVariable("DB_PASSWORD"),
        Database = database,
    }.ConnectionString;
}
