using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Configuration;

[Collection(EnvironmentVariablesCollection.Name)]
public class ConfigRepositoryTests : IDisposable
{
    private const string StringKey = "config_repository_string_key";
    private const string IntKey = "config_repository_int_key";
    private const string BoolKey = "config_repository_bool_key";

    private static readonly string[] Keys = [StringKey, IntKey, BoolKey];

    private readonly MainDbContext _context;
    private readonly Mock<ILogger<ConfigRepository>> _loggerMock = new();
    private readonly ConfigRepository _repository;
    private readonly Dictionary<string, string?> _originalEnvironmentVariables = new();

    public ConfigRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);
        _repository = new ConfigRepository(_context, _loggerMock.Object);

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

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldPreferEnvironmentValue_OverDatabaseValue()
    {
        await SeedAsync((StringKey, "from-db"));
        Environment.SetEnvironmentVariable(StringKey.ToUpperInvariant(), "from-env");

        var result = await _repository.GetConfigValueAsync<string>(StringKey);

        result.Should().Be("from-env");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldTrimEnvironmentValue()
    {
        Environment.SetEnvironmentVariable(IntKey.ToUpperInvariant(), "  42  ");

        var result = await _repository.GetConfigValueAsync<int>(IntKey);

        result.Should().Be(42);
        VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldFallBackToDatabase_WhenEnvironmentValueCannotBeConverted()
    {
        await SeedAsync((IntKey, "42"));
        Environment.SetEnvironmentVariable(IntKey.ToUpperInvariant(), "not-a-number");

        var result = await _repository.GetConfigValueAsync<int>(IntKey);

        result.Should().Be(42);
        _loggerMock.VerifyLog(LogLevel.Warning, "Environment variable CONFIG_REPOSITORY_INT_KEY has a value that cannot be read as Int32; using the stored setting instead");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldThrowConfigMissing_WhenKeyIsNotInDatabase()
    {
        var act = () => _repository.GetConfigValueAsync<string>(StringKey);

        var exception = await act.Should().ThrowAsync<ConfigMissingException>();
        exception.Which.ConfigKey.Should().Be(StringKey);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldThrowConfigMissing_WhenDatabaseValueCannotBeConverted()
    {
        await SeedAsync((BoolKey, "maybe"));

        var act = () => _repository.GetConfigValueAsync<bool>(BoolKey);

        var exception = await act.Should().ThrowAsync<ConfigMissingException>();
        exception.Which.ConfigKey.Should().Be(BoolKey);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldMatchKeyCaseInsensitively()
    {
        await SeedAsync((StringKey, "from-db"));

        var result = await _repository.GetConfigValueAsync<string>(StringKey.ToUpperInvariant());

        result.Should().Be("from-db");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueAsync_ShouldConvertBooleanValues()
    {
        await SeedAsync((BoolKey, "true"));

        var result = await _repository.GetConfigValueAsync<bool>(BoolKey);

        result.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllConfigsAsync_ShouldReturnEveryStoredEntry()
    {
        await SeedAsync((StringKey, "a"), (IntKey, "1"));

        var result = await _repository.GetAllConfigsAsync();

        result.Should().HaveCount(2);
        result[StringKey].Should().Be("a");
        result[IntKey].Should().Be("1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllConfigsAsync_ShouldReturnEmptyDictionary_WhenNothingIsStored()
    {
        var result = await _repository.GetAllConfigsAsync();

        result.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigsByPrefixAsync_ShouldOnlyReturnMatchingKeys()
    {
        await SeedAsync(("ai_provider", "ollama"), ("ai_base_url", "http://ollama:11434"), ("currency", "€"));

        var result = await _repository.GetConfigsByPrefixAsync("ai_");

        result.Should().HaveCount(2);
        result.Keys.Should().BeEquivalentTo(["ai_provider", "ai_base_url"]);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigsByPrefixAsync_ShouldReturnEmptyDictionary_WhenNothingMatches()
    {
        await SeedAsync(("currency", "€"));

        var result = await _repository.GetConfigsByPrefixAsync("ai_");

        result.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldInsertAllDefaults_WhenDatabaseIsEmpty()
    {
        var defaults = new List<ConfigDefault> { new(StringKey, "a"), new(IntKey, "1") };

        await _repository.SeedConfigAsync(defaults);

        var stored = await _context.Config.ToListAsync();
        stored.Should().HaveCount(2);
        stored.Select(c => c.Key).Should().BeEquivalentTo([StringKey, IntKey]);
        VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldNotTouchDatabase_WhenNothingIsMissing()
    {
        await SeedAsync((StringKey, "existing"));
        var saves = 0;
        _context.SavingChanges += (_, _) => saves++;

        await _repository.SeedConfigAsync([new ConfigDefault(StringKey, "default")]);

        saves.Should().Be(0);
        var stored = await _context.Config.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Value.Should().Be("existing");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldCompareKeysCaseInsensitively()
    {
        await SeedAsync((StringKey, "existing"));

        await _repository.SeedConfigAsync([new ConfigDefault(StringKey.ToUpperInvariant(), "default")]);

        var stored = await _context.Config.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Value.Should().Be("existing");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldDoNothing_WhenDefaultsAreEmpty()
    {
        await _repository.SeedConfigAsync([]);

        (await _context.Config.CountAsync()).Should().Be(0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueOrDefaultAsync_ShouldReturnTheStoredValue_WhenItExists()
    {
        await SeedAsync((IntKey, "42"));

        var result = await _repository.GetConfigValueOrDefaultAsync(IntKey, 7);

        result.Should().Be(42);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueOrDefaultAsync_ShouldReturnTheFallback_WhenTheKeyIsMissing()
    {
        var result = await _repository.GetConfigValueOrDefaultAsync(IntKey, 7);

        result.Should().Be(7);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueOrDefaultAsync_ShouldReturnTheFallback_WhenTheStoredValueCannotBeConverted()
    {
        await SeedAsync((IntKey, "not-a-number"));

        var result = await _repository.GetConfigValueOrDefaultAsync(IntKey, 7);

        result.Should().Be(7);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetConfigValueOrDefaultAsync_ShouldPreferTheEnvironmentValue()
    {
        await SeedAsync((IntKey, "42"));
        Environment.SetEnvironmentVariable(IntKey.ToUpperInvariant(), "9");

        var result = await _repository.GetConfigValueOrDefaultAsync(IntKey, 7);

        result.Should().Be(9);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldOnlyAddTheMissingDefaults()
    {
        await SeedAsync((StringKey, "existing"));

        await _repository.SeedConfigAsync([new ConfigDefault(StringKey, "default"), new ConfigDefault(IntKey, "5")]);

        var stored = await _context.Config.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value);
        stored.Should().BeEquivalentTo(new Dictionary<string, string> { [StringKey] = "existing", [IntKey] = "5" });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedConfigAsync_ShouldStoreAKeyThatCanBeReadBack_WhenTheDefaultKeyIsUpperCase()
    {
        await _repository.SeedConfigAsync([new ConfigDefault(StringKey.ToUpperInvariant(), "default")]);

        var result = await _repository.GetConfigValueAsync<string>(StringKey);

        result.Should().Be("default");
        VerifyNoOtherCalls();
    }
}
