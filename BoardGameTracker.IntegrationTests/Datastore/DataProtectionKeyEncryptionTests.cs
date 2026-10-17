using System.Security.Cryptography;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class DataProtectionKeyEncryptionTests
{
    private const string Purpose = "integration-data-protection";
    private const string Payload = "integration-protected-secret";

    private readonly IntegrationFixture _fixture;

    public DataProtectionKeyEncryptionTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task KeysCreatedByTheApp_ShouldBeStoredEncrypted_AndStayReadableFromANewServiceProvider()
    {
        string protectedPayload;
        await using (var scope = _fixture.CreateScope())
        {
            protectedPayload = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Protect(Payload);
        }

        var stored = await ReadKeyXmlAsync(_fixture.GetConnectionString("boardgametracker"));

        stored.Should().NotBeEmpty();
        stored.Should().AllSatisfy(xml => xml.Should().NotContain("<masterKey").And.Contain("encryptedSecret").And.Contain(nameof(SecretXmlDecryptor)));

        await using var provider = BuildProvider(_fixture.GetConnectionString("boardgametracker"), IntegrationFixture.DataProtectionKey);
        provider.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Unprotect(protectedPayload).Should().Be(Payload);
    }

    [Fact]
    public async Task PlaintextKeysFromBeforeTheVariableWasSet_ShouldBeReEncrypted_AndKeepTheirDataReadable()
    {
        var connectionString = await CreateDatabaseAsync();

        string legacyPayload;
        await using (var legacy = BuildProvider(connectionString, null))
        {
            legacyPayload = legacy.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Protect(Payload);
        }

        (await ReadKeyXmlAsync(connectionString)).Should().ContainSingle().Which.Should().Contain("<masterKey");

        await using (var upgraded = BuildProvider(connectionString, IntegrationFixture.DataProtectionKey))
        {
            await using var scope = upgraded.CreateAsyncScope();
            var reEncryptor = ActivatorUtilities.CreateInstance<DataProtectionKeyReEncryptor>(scope.ServiceProvider);
            (await reEncryptor.EncryptPlaintextKeysAsync()).Should().Be(1);
            (await reEncryptor.EncryptPlaintextKeysAsync()).Should().Be(0);
            upgraded.GetRequiredService<IKeyManager>().CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
        }

        var stored = await ReadKeyXmlAsync(connectionString);
        stored.Should().HaveCount(2);
        stored.Should().AllSatisfy(xml => xml.Should().NotContain("<masterKey").And.Contain("encryptedSecret"));

        await using (var restarted = BuildProvider(connectionString, IntegrationFixture.DataProtectionKey))
        {
            var protector = restarted.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose);
            protector.Unprotect(legacyPayload).Should().Be(Payload);
            protector.Unprotect(protector.Protect(Payload)).Should().Be(Payload);
        }

        await using (var withoutKey = BuildProvider(connectionString, null))
        {
            var act = () => withoutKey.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Unprotect(legacyPayload);
            act.Should().Throw<CryptographicException>();
        }

        await using (var withOtherKey = BuildProvider(connectionString, IntegrationFixture.DataProtectionKey + "-changed"))
        {
            var protector = withOtherKey.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose);
            var act = () => protector.Unprotect(legacyPayload);
            act.Should().Throw<CryptographicException>();
            protector.Unprotect(protector.Protect(Payload)).Should().Be(Payload);
        }
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var database = $"dpkeys_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(_fixture.GetConnectionString("postgres")))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        var connectionString = _fixture.GetConnectionString(database);
        await using var provider = BuildProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MainDbContext>().Database.MigrateAsync();
        return connectionString;
    }

    private static async Task<List<string>> ReadKeyXmlAsync(string connectionString)
    {
        await using var provider = BuildProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MainDbContext>().DataProtectionKeys
            .AsNoTracking()
            .OrderBy(key => key.Id)
            .Select(key => key.Xml!)
            .ToListAsync();
    }

    private static ServiceProvider BuildProvider(string connectionString, string? secret)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_ =>
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.UseVector();
            return builder.Build();
        });
        services.AddDbContext<MainDbContext>((serviceProvider, options) =>
            options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>(), npgsql => npgsql.UseVector()));
        services.AddDataProtection()
            .PersistKeysToDbContext<MainDbContext>()
            .SetApplicationName("boardgametracker")
            .ProtectKeysWithSecret(secret);
        return services.BuildServiceProvider();
    }
}
