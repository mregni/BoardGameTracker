using System.Collections.Concurrent;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Settings;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class SensitiveDataLoggingTests
{
    private const string BggKey = "bgg-key-that-must-never-be-logged-7f3a";
    private const string ChangeDetectionKey = "cd-key-that-must-never-be-logged-91c2";

    private readonly IntegrationFixture _fixture;

    public SensitiveDataLoggingTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SavingSettings_ShouldNotWriteApiKeysToTheEfLog()
    {
        var log = await SaveKeysAndCaptureLogAsync(sensitiveDataLogging: false);

        log.Should().Contain(line => line.Contains("Executed DbCommand"));
        log.Should().NotContain(line => line.Contains(BggKey));
        log.Should().NotContain(line => line.Contains(ChangeDetectionKey));
    }

    [Fact]
    public async Task SavingSettings_ShouldWriteApiKeysToTheEfLog_OnlyWhenSensitiveDataLoggingIsForced()
    {
        var log = await SaveKeysAndCaptureLogAsync(sensitiveDataLogging: true);

        log.Should().Contain(line => line.Contains(BggKey));
    }

    private async Task<List<string>> SaveKeysAndCaptureLogAsync(bool sensitiveDataLogging)
    {
        var lines = new ConcurrentQueue<string>();
        await using var scope = _fixture.CreateScope();
        var configured = scope.ServiceProvider.GetRequiredService<DbContextOptions<MainDbContext>>();
        var builder = new DbContextOptionsBuilder<MainDbContext>(configured)
            .LogTo(lines.Enqueue, LogLevel.Debug);
        if (sensitiveDataLogging)
        {
            builder.EnableSensitiveDataLogging();
        }

        await using var context = new MainDbContext(builder.Options);
        var settingsService = new SettingsService(
            new ConfigRepository(context, NullLogger<ConfigRepository>.Instance),
            scope.ServiceProvider.GetRequiredService<IEnvironmentProvider>(),
            new UnitOfWork(context),
            NullLogger<SettingsService>.Instance);

        var settings = await settingsService.GetSettingsAsync();
        try
        {
            settings.BggApiKey = BggKey;
            settings.ChangeDetectionApiKey = ChangeDetectionKey;
            await settingsService.UpdateSettingsAsync(settings);
        }
        finally
        {
            settings.BggApiKey = null;
            settings.ChangeDetectionApiKey = null;
            await settingsService.UpdateSettingsAsync(settings);
        }

        return [.. lines];
    }
}
