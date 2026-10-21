using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(Infrastructure.IntegrationCollection.Name)]
public class PlayerBadgeAwardedAtMigrationTests : IAsyncLifetime
{
    private const string BeforeAwardedAt = "20261021130839_AddPlayerImageSource";

    private const string SeedHistory = """
        INSERT INTO "Players" ("Id", "Name") VALUES (1, 'Veteran'), (2, 'Cleaned up');
        INSERT INTO "Games" ("Id", "Title", "Description", "State", "HasScoring", "HasCoopMode", "CoopByDefault") VALUES
            (1, 'Alpha', '', 0, true, false, false),
            (2, 'Beta', '', 0, true, false, false),
            (3, 'Gamma', '', 0, true, false, false);
        INSERT INTO "Sessions" ("Id", "GameId", "Comment", "IsCoop", "Start", "End") VALUES
            (1, 1, '', false, '2026-01-01T10:00:00Z', '2026-01-01T11:00:00Z'),
            (2, 2, '', false, '2026-01-02T10:00:00Z', '2026-01-02T11:00:00Z'),
            (3, 1, '', false, '2026-01-03T10:00:00Z', '2026-01-03T11:00:00Z'),
            (4, 3, '', false, '2026-01-04T10:00:00Z', '2026-01-04T11:00:00Z'),
            (5, 2, '', false, '2026-01-05T10:00:00Z', '2026-01-05T11:00:00Z'),
            (6, 1, '', false, '2026-01-06T10:00:00Z', '2026-01-06T11:00:00Z');
        INSERT INTO "PlayerSessions" ("PlayerId", "SessionId", "Won", "FirstPlay") VALUES
            (1, 1, true, true),
            (1, 2, true, true),
            (1, 3, true, false),
            (1, 4, false, true),
            (1, 5, false, false),
            (2, 6, false, true);
        INSERT INTO "BadgePlayer" ("BadgesId", "PlayersId") VALUES
            (1, 1),
            (5, 1),
            (9, 1),
            (36, 1),
            (5, 2);
        """;

    private readonly string _database = "player_badge_awarded_at_" + Guid.NewGuid().ToString("N");
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
    public async Task Upgrading_ShouldDateMilestoneBadgesFromTheSessionThatReachedThem()
    {
        await using (var context = CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(BeforeAwardedAt, TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(SeedHistory, TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            var awarded = await context.Set<PlayerBadge>()
                .AsNoTracking()
                .ToDictionaryAsync(x => (x.PlayerId, x.BadgeId), x => x.AwardedAt, TestContext.Current.CancellationToken);

            awarded[(1, 1)].Should().Be(Utc(2026, 1, 4));
            awarded[(1, 5)].Should().Be(Utc(2026, 1, 5));
            awarded[(1, 9)].Should().Be(Utc(2026, 1, 3));
            awarded[(1, 36)].Should().BeNull();
            awarded[(2, 5)].Should().BeNull();
        }
    }

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 10, 0, 0, DateTimeKind.Utc);

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
