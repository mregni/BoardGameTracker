using System.Collections.Concurrent;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Settings;
using BoardGameTracker.Core.Settings.Interfaces;
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
    public async Task SavingSettings_ShouldNotWriteApiKeysOrTheirCiphertextToTheEfLog()
    {
        var capture = await SaveKeysAndCaptureLogAsync(sensitiveDataLogging: false);

        capture.StoredBggKey.Should().StartWith("dp1:");
        capture.StoredChangeDetectionKey.Should().StartWith("dp1:");
        capture.Log.Should().Contain(line => line.Contains("Executed DbCommand"));
        capture.Log.Should().NotContain(line => line.Contains(BggKey));
        capture.Log.Should().NotContain(line => line.Contains(ChangeDetectionKey));
        capture.Log.Should().NotContain(line => line.Contains(capture.StoredBggKey));
        capture.Log.Should().NotContain(line => line.Contains(capture.StoredChangeDetectionKey));
    }

    [Fact]
    public async Task SavingSettings_ShouldWriteTheStoredKeyToTheEfLog_OnlyWhenSensitiveDataLoggingIsForced()
    {
        var capture = await SaveKeysAndCaptureLogAsync(sensitiveDataLogging: true);

        capture.StoredBggKey.Should().StartWith("dp1:");
        capture.Log.Should().Contain(line => line.Contains(capture.StoredBggKey));
    }

    private async Task<LogCapture> SaveKeysAndCaptureLogAsync(bool sensitiveDataLogging)
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
            scope.ServiceProvider.GetRequiredService<IIntegrationKeyProtector>(),
            NullLogger<SettingsService>.Instance);

        var settings = await settingsService.GetSettingsAsync();
        try
        {
            settings.BggApiKey = BggKey;
            settings.ChangeDetectionApiKey = ChangeDetectionKey;
            await settingsService.UpdateSettingsAsync(settings);

            await using var readScope = _fixture.CreateScope();
            var stored = await readScope.ServiceProvider.GetRequiredService<IConfigRepository>().GetAllConfigsAsync();
            return new LogCapture(
                [.. lines],
                stored[Constants.BggConfig.ApiKey.ToLowerInvariant()],
                stored[Constants.ChangeDetectionConfig.ApiKey.ToLowerInvariant()]);
        }
        finally
        {
            settings.BggApiKey = null;
            settings.ChangeDetectionApiKey = null;
            await settingsService.UpdateSettingsAsync(settings);
        }
    }

    private sealed record LogCapture(List<string> Log, string StoredBggKey, string StoredChangeDetectionKey);
}
