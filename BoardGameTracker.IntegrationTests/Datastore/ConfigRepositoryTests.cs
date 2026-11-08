using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class ConfigRepositoryTests
{
    private readonly IntegrationFixture _fixture;

    public ConfigRepositoryTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReplaceConfigValueAsync_ShouldUpdateTheValue_WhenTheStoredValueIsTheExpectedOne()
    {
        var key = UniqueKey();
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        await repository.SetConfigValueAsync(key, "old");

        var replaced = await repository.ReplaceConfigValueAsync(key.ToUpperInvariant(), "old", "new");

        replaced.Should().BeTrue();
        (await ReadValuesAsync(key)).Should().Equal("new");
    }

    [Fact]
    public async Task ReplaceConfigValueAsync_ShouldLeaveTheValue_WhenTheStoredValueChangedMeanwhile()
    {
        var key = UniqueKey();
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        await repository.SetConfigValueAsync(key, "changed");

        var replaced = await repository.ReplaceConfigValueAsync(key, "old", "new");

        replaced.Should().BeFalse();
        (await ReadValuesAsync(key)).Should().Equal("changed");
    }

    [Fact]
    public async Task ReplaceConfigValueAsync_ShouldReturnFalse_AndInsertNothing_WhenTheKeyIsMissing()
    {
        var key = UniqueKey();
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();

        var replaced = await repository.ReplaceConfigValueAsync(key, string.Empty, "new");

        replaced.Should().BeFalse();
        (await ReadValuesAsync(key)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetConfigsByPrefixAsync_ShouldTreatAnUnderscoreInThePrefixLiterally()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        await repository.SetConfigValueAsync($"prefix_{suffix}", "match");
        await repository.SetConfigValueAsync($"prefixx{suffix}", "wildcard");

        var configs = await repository.GetConfigsByPrefixAsync("prefix_");

        configs.Should().Contain($"prefix_{suffix}", "match");
        configs.Should().NotContainKey($"prefixx{suffix}");
    }

    [Fact]
    public async Task SetConfigValueAsync_ShouldUpdateTheRow_WhenAConcurrentInsertOfTheSameKeyWinsTheRace()
    {
        var token = TestContext.Current.CancellationToken;
        var key = UniqueKey();
        await using var competitor = _fixture.CreateScope();
        var competitorDb = competitor.ServiceProvider.GetRequiredService<MainDbContext>();
        await using var competitorTransaction = await competitorDb.Database.BeginTransactionAsync(token);
        competitorDb.Config.Add(new Config { Key = key, Value = "competitor" });
        await competitorDb.SaveChangesAsync(token);

        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        var set = repository.SetConfigValueAsync(key, "winner");
        await WaitForABlockedQueryAsync(set);
        await competitorTransaction.CommitAsync(token);
        await set;

        (await ReadValuesAsync(key)).Should().Equal("winner");
    }

    private async Task WaitForABlockedQueryAsync(Task running)
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var blocked = 0;
        while (blocked == 0 && DateTime.UtcNow < deadline && !running.IsCompleted)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
            blocked = await db.Database
                .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_stat_activity WHERE wait_event_type = 'Lock' AND datname = current_database()")
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        blocked.Should().BePositive("the insert should wait for the competing transaction");
    }

    private async Task<List<string>> ReadValuesAsync(string key)
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await db.Config.AsNoTracking().Where(c => c.Key == key).Select(c => c.Value).ToListAsync(TestContext.Current.CancellationToken);
    }

    private static string UniqueKey() => $"integration_{Guid.NewGuid():N}";
}
