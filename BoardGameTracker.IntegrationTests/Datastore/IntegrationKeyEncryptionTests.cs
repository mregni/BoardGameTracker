using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class IntegrationKeyEncryptionTests : IAsyncLifetime
{
    private const string BggKey = "integration-encrypted-bgg-key";
    private const string ChangeDetectionKey = "integration-encrypted-cd-key";

    private readonly IntegrationFixture _fixture;

    public IntegrationKeyEncryptionTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        await repository.SetConfigValueAsync(Constants.BggConfig.ApiKey, string.Empty);
        await repository.SetConfigValueAsync(Constants.ChangeDetectionConfig.ApiKey, string.Empty);
    }

    [Fact]
    public async Task SavingKeys_ShouldStoreThemEncrypted_AndTheServiceShouldReadThemBack()
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        var settings = await admin.GetFromJsonAsync<UIResourceDto>("/api/settings", IntegrationFixture.Json);
        settings!.BggApiKey = BggKey;
        settings.ChangeDetectionApiKey = ChangeDetectionKey;

        using var response = await admin.PutAsJsonAsync("/api/settings", settings, IntegrationFixture.Json);
        var body = await response.Content.ReadAsStringAsync();
        using var reread = await admin.GetAsync("/api/settings");
        var rereadBody = await reread.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        body.Should().NotContain(BggKey).And.NotContain(ChangeDetectionKey);
        rereadBody.Should().NotContain(BggKey).And.NotContain(ChangeDetectionKey);

        var bggRow = await ReadRowAsync(Constants.BggConfig.ApiKey);
        var changeDetectionRow = await ReadRowAsync(Constants.ChangeDetectionConfig.ApiKey);
        bggRow.Should().StartWith("dp1:").And.NotContain(BggKey);
        changeDetectionRow.Should().StartWith("dp1:").And.NotContain(ChangeDetectionKey);

        await using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        (await service.GetBggApiKeyAsync()).Should().Be(BggKey);
        (await service.GetChangeDetectionSettingsAsync()).ApiKey.Should().Be(ChangeDetectionKey);
        var status = await service.GetSettingsAsync();
        status.BggStatus.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task LegacyPlaintextRows_ShouldBeEncryptedOnce_AndStayReadable()
    {
        await using (var seed = _fixture.CreateScope())
        {
            var repository = seed.ServiceProvider.GetRequiredService<IConfigRepository>();
            await repository.SetConfigValueAsync(Constants.BggConfig.ApiKey, BggKey);
            await repository.SetConfigValueAsync(Constants.ChangeDetectionConfig.ApiKey, ChangeDetectionKey);
        }

        await using (var beforeMigration = _fixture.CreateScope())
        {
            var service = beforeMigration.ServiceProvider.GetRequiredService<ISettingsService>();
            (await service.GetBggApiKeyAsync()).Should().Be(BggKey);
        }

        int firstRun;
        int secondRun;
        await using (var scope = _fixture.CreateScope())
        {
            var migrator = scope.ServiceProvider.GetRequiredService<IIntegrationKeyMigrator>();
            firstRun = await migrator.ProtectLegacyKeysAsync();
        }

        var bggAfterFirstRun = await ReadRowAsync(Constants.BggConfig.ApiKey);
        var changeDetectionAfterFirstRun = await ReadRowAsync(Constants.ChangeDetectionConfig.ApiKey);

        await using (var scope = _fixture.CreateScope())
        {
            var migrator = scope.ServiceProvider.GetRequiredService<IIntegrationKeyMigrator>();
            secondRun = await migrator.ProtectLegacyKeysAsync();
        }

        firstRun.Should().Be(2);
        secondRun.Should().Be(0);
        bggAfterFirstRun.Should().StartWith("dp1:").And.NotContain(BggKey);
        changeDetectionAfterFirstRun.Should().StartWith("dp1:").And.NotContain(ChangeDetectionKey);
        (await ReadRowAsync(Constants.BggConfig.ApiKey)).Should().Be(bggAfterFirstRun);
        (await ReadRowAsync(Constants.ChangeDetectionConfig.ApiKey)).Should().Be(changeDetectionAfterFirstRun);

        await using var read = _fixture.CreateScope();
        var settings = read.ServiceProvider.GetRequiredService<ISettingsService>();
        (await settings.GetBggApiKeyAsync()).Should().Be(BggKey);
        (await settings.GetChangeDetectionSettingsAsync()).ApiKey.Should().Be(ChangeDetectionKey);
    }

    [Fact]
    public async Task UndecryptableRows_ShouldBeTreatedAsNotConfigured_WithoutFailingTheRequest()
    {
        await using (var seed = _fixture.CreateScope())
        {
            var repository = seed.ServiceProvider.GetRequiredService<IConfigRepository>();
            await repository.SetConfigValueAsync(Constants.BggConfig.ApiKey, "dp1:not-a-valid-payload");
        }

        using var admin = await _fixture.CreateAdminClientAsync();
        var settings = await admin.GetFromJsonAsync<UIResourceDto>("/api/settings", IntegrationFixture.Json);

        settings!.BggStatus.IsConfigured.Should().BeFalse();
        await using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        (await service.GetBggApiKeyAsync()).Should().BeNull();
        (await service.IsBggEnabled()).Should().BeFalse();
    }

    private async Task<string> ReadRowAsync(string key)
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await db.Config.AsNoTracking().Where(c => c.Key == key).Select(c => c.Value).SingleAsync();
    }
}
