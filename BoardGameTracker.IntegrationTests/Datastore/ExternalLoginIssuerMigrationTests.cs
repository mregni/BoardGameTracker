using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(Infrastructure.IntegrationCollection.Name)]
public class ExternalLoginIssuerMigrationTests : IAsyncLifetime
{
    private const string BeforeIssuerKeys = "20261015230803_AddCoopGames";

    private const string SeedLegacyRows = """
        INSERT INTO auth."AspNetUsers" ("Id", "UserName", "CreatedAt", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount", "PasswordHash")
        VALUES ('sso-user', 'sso', now(), false, false, false, true, 0, NULL),
               ('local-user', 'local', now(), false, false, false, true, 0, 'hash'),
               ('orphan-user', 'orphan', now(), false, false, false, true, 0, NULL),
               ('plain-user', 'plain', now(), false, false, false, true, 0, NULL);
        INSERT INTO auth."OidcProviders" ("Name", "DisplayName", "Enabled", "Authority", "ClientId", "Scopes", "AutoProvisionUsers")
        VALUES ('keycloak', 'Keycloak', true, ' https://sso.example.com/realms/bgt/ ', 'bgt', 'openid', true);
        INSERT INTO auth."ExternalLogins" ("UserId", "Provider", "ProviderKey", "LinkedAt")
        VALUES ('sso-user', 'keycloak', 'sub-1', now()),
               ('local-user', 'keycloak', 'sub-2', now()),
               ('orphan-user', 'deleted-provider', 'sub-1', now());
        """;

    private const string BeforeProviderNameDrop = "20261019015719_AddGameNightAttendance";

    private const string SeedNameKeyedRows = """
        INSERT INTO auth."AspNetUsers" ("Id", "UserName", "CreatedAt", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount", "PasswordHash", "RoleManagedByIdentityProvider")
        VALUES ('sso-user', 'sso', now(), false, false, false, true, 0, NULL, true),
               ('local-user', 'local', now(), false, false, false, true, 0, 'hash', false),
               ('orphan-user', 'orphan', now(), false, false, false, true, 0, NULL, true);
        INSERT INTO auth."OidcProviders" ("Name", "DisplayName", "Enabled", "Authority", "ClientId", "Scopes", "AutoProvisionUsers", "DefaultRole")
        VALUES ('keycloak', 'Company login', true, 'https://sso.example.com/realms/bgt/', 'bgt', 'openid', true, 'User');
        INSERT INTO auth."ExternalLogins" ("UserId", "Provider", "Issuer", "ProviderKey", "LinkedAt")
        VALUES ('sso-user', 'keycloak', NULL, 'sub-1', now()),
               ('local-user', 'keycloak', 'https://sso.example.com/realms/bgt', 'sub-2', now()),
               ('orphan-user', 'deleted-provider', NULL, 'sub-3', now());
        """;

    private readonly string _database = "issuer_migration_" + Guid.NewGuid().ToString("N");
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
    public async Task Upgrading_ShouldKeyExistingLinksOnTheProvidersAuthority_AndMarkSsoOnlyUsersAsIdentityProviderManaged()
    {
        await using (var context = CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(BeforeIssuerKeys, TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(SeedLegacyRows, TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            var logins = await context.ExternalLogins.AsNoTracking().OrderBy(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken);
            logins.Should().HaveCount(3);
            logins.Single(x => x.UserId == "sso-user").Issuer.Should().Be("https://sso.example.com/realms/bgt");
            logins.Single(x => x.UserId == "local-user").Issuer.Should().Be("https://sso.example.com/realms/bgt");
            logins.Single(x => x.UserId == "orphan-user").Issuer.Should().BeNull();

            var users = await context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.RoleManagedByIdentityProvider, TestContext.Current.CancellationToken);
            users["sso-user"].Should().BeTrue();
            users["orphan-user"].Should().BeTrue();
            users["local-user"].Should().BeFalse();
            users["plain-user"].Should().BeFalse();

            (await context.OidcProviders.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).DefaultRole.Should().Be("User");
        }
    }

    [Fact]
    public async Task DroppingTheProviderName_ShouldKeyLegacyLinksOnTheAuthority_AndLabelLinksWithTheDisplayName()
    {
        await using (var context = CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(BeforeProviderNameDrop, TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(SeedNameKeyedRows, TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = CreateContext())
        {
            var logins = await context.ExternalLogins.AsNoTracking().ToDictionaryAsync(x => x.UserId, TestContext.Current.CancellationToken);
            logins["sso-user"].Issuer.Should().Be("https://sso.example.com/realms/bgt");
            logins["sso-user"].Provider.Should().Be("Company login");
            logins["local-user"].Issuer.Should().Be("https://sso.example.com/realms/bgt");
            logins["local-user"].Provider.Should().Be("Company login");
            logins["orphan-user"].Issuer.Should().BeNull();
            logins["orphan-user"].Provider.Should().Be("deleted-provider");
            (await context.OidcProviders.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).DisplayName.Should().Be("Company login");
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
