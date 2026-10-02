using BoardGameTracker.Common.Entities;
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
public class DataMigrationTests : IAsyncLifetime
{
    private const string BeforeDataRepairs = "20260827220101_AddChangeDetectionWatchIdToGame";

    private readonly string _database = "migrations_" + Guid.NewGuid().ToString("N");
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
    public async Task UpgradingAPopulatedDatabase_ShouldMergeDuplicates_KeepTheNewestConfig_AndClearEmptyGameMetadata()
    {
        int gameA;
        int gameB;
        await using (var context = CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(BeforeDataRepairs, TestContext.Current.CancellationToken);

            var a = new Game("Brass", true, GameState.Owned);
            var b = new Game("Ark Nova", true, GameState.Owned);
            a.AddCategory(new GameCategory("Strategy"));
            b.AddCategory(new GameCategory("Strategy"));
            a.AddMechanic(new GameMechanic("Hand Management"));
            b.AddMechanic(new GameMechanic("Hand Management"));
            a.AddPerson(new Person("Martin Wallace", PersonType.Designer));
            b.AddPerson(new Person("Martin Wallace", PersonType.Designer));
            a.UpdatePlayTime(0, 0);
            context.Games.AddRange(a, b);
            context.Config.Add(new Config { Key = "currency", Value = "EUR" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.Config.Add(new Config { Key = "currency", Value = "USD" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync("UPDATE \"Games\" SET \"MinAge\" = 0", TestContext.Current.CancellationToken);
            gameA = a.Id;
            gameB = b.Id;
        }

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            var games = await context.Games.AsNoTracking()
                .Include(g => g.Categories)
                .Include(g => g.Mechanics)
                .Include(g => g.People)
                .Where(g => g.Id == gameA || g.Id == gameB)
                .ToListAsync(TestContext.Current.CancellationToken);

            games.Should().HaveCount(2);
            games.Should().AllSatisfy(g =>
            {
                g.Categories.Should().ContainSingle(c => c.Name == "Strategy");
                g.Mechanics.Should().ContainSingle(m => m.Name == "Hand Management");
                g.People.Should().ContainSingle(p => p.Name == "Martin Wallace");
                g.MinAge.Should().BeNull();
            });
            games.Single(g => g.Id == gameA).PlayTime.Should().BeNull();

            (await context.GameCategories.CountAsync(c => c.Name == "Strategy", TestContext.Current.CancellationToken)).Should().Be(1);
            (await context.GameMechanics.CountAsync(m => m.Name == "Hand Management", TestContext.Current.CancellationToken)).Should().Be(1);
            (await context.People.CountAsync(p => p.Name == "Martin Wallace", TestContext.Current.CancellationToken)).Should().Be(1);
            (await context.Config.Where(c => c.Key == "currency").Select(c => c.Value).ToListAsync(TestContext.Current.CancellationToken))
                .Should().Equal("USD");
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
