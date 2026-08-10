using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Configuration;

[Collection("EnvironmentVariables")]
public class ConfigRepositoryTests : IDisposable
{
    private const string StringKey = "config_repository_string_key";
    private const string IntKey = "config_repository_int_key";
    private const string BoolKey = "config_repository_bool_key";

    private static readonly string[] Keys = [StringKey, IntKey, BoolKey];

    private readonly MainDbContext _context;
    private readonly ConfigRepository _repository;
    private readonly Dictionary<string, string?> _originalEnvironmentVariables = new();

    public ConfigRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);
        _repository = new ConfigRepository(_context);

        foreach (var key in Keys)
        {
            var variable = key.ToUpperInvariant();
            _originalEnvironmentVariables[variable] = Environment.GetEnvironmentVariable(variable);
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SeedAsync(params (string Key, string Value)[] entries)
    {
        foreach (var entry in entries)
        {
            _context.Config.Add(new Config { Key = entry.Key, Value = entry.Value });
        }
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldReturnDatabaseValue_WhenNoEnvironmentOverrideExists()
    {
        await SeedAsync((StringKey, "from-db"));

        var result = await _repository.GetConfigValueAsync<string>(StringKey);

        result.Should().Be("from-db");
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldPreferEnvironmentValue_OverDatabaseValue()
    {
        await SeedAsync((StringKey, "from-db"));
        Environment.SetEnvironmentVariable(StringKey.ToUpperInvariant(), "from-env");

        var result = await _repository.GetConfigValueAsync<string>(StringKey);

        result.Should().Be("from-env");
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldTrimEnvironmentValue()
    {
        Environment.SetEnvironmentVariable(IntKey.ToUpperInvariant(), "  42  ");

        var result = await _repository.GetConfigValueAsync<int>(IntKey);

        result.Should().Be(42);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConfigValueAsync_ShouldFallBackToDatabase_WhenEnvironmentValueIsBlank(string environmentValue)
    {
        await SeedAsync((StringKey, "from-db"));
        Environment.SetEnvironmentVariable(StringKey.ToUpperInvariant(), environmentValue);

        var result = await _repository.GetConfigValueAsync<string>(StringKey);

        result.Should().Be("from-db");
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldThrowConfigMissing_WhenEnvironmentValueCannotBeConverted()
    {
        await SeedAsync((IntKey, "42"));
        Environment.SetEnvironmentVariable(IntKey.ToUpperInvariant(), "not-a-number");

        var act = () => _repository.GetConfigValueAsync<int>(IntKey);

        var exception = await act.Should().ThrowAsync<ConfigMissingException>();
        exception.Which.ConfigKey.Should().Be(IntKey);
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldThrowConfigMissing_WhenKeyIsNotInDatabase()
    {
        var act = () => _repository.GetConfigValueAsync<string>(StringKey);

        var exception = await act.Should().ThrowAsync<ConfigMissingException>();
        exception.Which.ConfigKey.Should().Be(StringKey);
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldThrowConfigMissing_WhenDatabaseValueCannotBeConverted()
    {
        await SeedAsync((BoolKey, "maybe"));

        var act = () => _repository.GetConfigValueAsync<bool>(BoolKey);

        var exception = await act.Should().ThrowAsync<ConfigMissingException>();
        exception.Which.ConfigKey.Should().Be(BoolKey);
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldMatchKeyCaseInsensitively()
    {
        await SeedAsync((StringKey, "from-db"));

        var result = await _repository.GetConfigValueAsync<string>(StringKey.ToUpperInvariant());

        result.Should().Be("from-db");
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldConvertBooleanValues()
    {
        await SeedAsync((BoolKey, "true"));

        var result = await _repository.GetConfigValueAsync<bool>(BoolKey);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllConfigsAsync_ShouldReturnEveryStoredEntry()
    {
        await SeedAsync((StringKey, "a"), (IntKey, "1"));

        var result = await _repository.GetAllConfigsAsync();

        result.Should().HaveCount(2);
        result[StringKey].Should().Be("a");
        result[IntKey].Should().Be("1");
    }

    [Fact]
    public async Task GetAllConfigsAsync_ShouldReturnEmptyDictionary_WhenNothingIsStored()
    {
        var result = await _repository.GetAllConfigsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConfigsByPrefixAsync_ShouldOnlyReturnMatchingKeys()
    {
        await SeedAsync(("ai_provider", "ollama"), ("ai_base_url", "http://ollama:11434"), ("currency", "€"));

        var result = await _repository.GetConfigsByPrefixAsync("ai_");

        result.Should().HaveCount(2);
        result.Keys.Should().BeEquivalentTo(["ai_provider", "ai_base_url"]);
    }

    [Fact]
    public async Task GetConfigsByPrefixAsync_ShouldReturnEmptyDictionary_WhenNothingMatches()
    {
        await SeedAsync(("currency", "€"));

        var result = await _repository.GetConfigsByPrefixAsync("ai_");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldInsertAllDefaults_WhenDatabaseIsEmpty()
    {
        var defaults = new List<ConfigDefault> { new(StringKey, "a"), new(IntKey, "1") };

        await _repository.SeedConfigAsync(defaults);

        var stored = await _context.Config.ToListAsync();
        stored.Should().HaveCount(2);
        stored.Select(c => c.Key).Should().BeEquivalentTo([StringKey, IntKey]);
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldNotOverwriteExistingValues()
    {
        await SeedAsync((StringKey, "existing"));
        var defaults = new List<ConfigDefault> { new(StringKey, "default"), new(IntKey, "1") };

        await _repository.SeedConfigAsync(defaults);

        var stored = await _context.Config.ToDictionaryAsync(c => c.Key, c => c.Value);
        stored.Should().HaveCount(2);
        stored[StringKey].Should().Be("existing");
        stored[IntKey].Should().Be("1");
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldNotTouchDatabase_WhenNothingIsMissing()
    {
        await SeedAsync((StringKey, "existing"));

        await _repository.SeedConfigAsync([new ConfigDefault(StringKey, "default")]);

        var stored = await _context.Config.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Value.Should().Be("existing");
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldCompareKeysCaseInsensitively()
    {
        await SeedAsync((StringKey, "existing"));

        await _repository.SeedConfigAsync([new ConfigDefault(StringKey.ToUpperInvariant(), "default")]);

        var stored = await _context.Config.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Value.Should().Be("existing");
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldDoNothing_WhenDefaultsAreEmpty()
    {
        await _repository.SeedConfigAsync([]);

        (await _context.Config.CountAsync()).Should().Be(0);
    }
}
