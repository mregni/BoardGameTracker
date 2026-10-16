using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Settings;

[Collection(EnvironmentVariablesCollection.Name)]
public class IntegrationKeyMigratorTests : IDisposable
{
    private const string BggPlaintext = "legacy-bgg-key";
    private const string ChangeDetectionPlaintext = "legacy-cd-key";
    private const string BggProtected = "dp1:bgg-cipher";
    private const string ChangeDetectionProtected = "dp1:cd-cipher";

    private readonly string? _originalBggEnvironmentValue = Environment.GetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName);
    private readonly Mock<IConfigRepository> _configRepositoryMock = new();
    private readonly Mock<IIntegrationKeyProtector> _protectorMock = new();
    private readonly Mock<ILogger<IntegrationKeyMigrator>> _loggerMock = new();
    private readonly IntegrationKeyMigrator _migrator;

    public IntegrationKeyMigratorTests()
    {
        Environment.SetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName, null);
        _protectorMock.Setup(x => x.IsProtected(It.IsAny<string>()))
            .Returns<string>(value => value.StartsWith("dp1:", StringComparison.Ordinal));
        _protectorMock.Setup(x => x.Protect(BggPlaintext)).Returns(BggProtected);
        _protectorMock.Setup(x => x.Protect(ChangeDetectionPlaintext)).Returns(ChangeDetectionProtected);
        _protectorMock.Setup(x => x.Reveal(Constants.BggConfig.ApiKey, BggProtected)).Returns(BggPlaintext);
        _protectorMock.Setup(x => x.Reveal(Constants.ChangeDetectionConfig.ApiKey, ChangeDetectionProtected)).Returns(ChangeDetectionPlaintext);
        _configRepositoryMock
            .Setup(x => x.ReplaceConfigValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _migrator = new IntegrationKeyMigrator(_configRepositoryMock.Object, _protectorMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName, _originalBggEnvironmentValue);
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _configRepositoryMock.VerifyNoOtherCalls();
        _protectorMock.VerifyNoOtherCalls();
    }

    private void SetupStored(Dictionary<string, string> configs)
    {
        _configRepositoryMock.Setup(x => x.GetAllConfigsAsync()).ReturnsAsync(configs);
    }

    [Fact]
    public async Task ProtectLegacyKeysAsync_ShouldEncryptPlaintextKeys_AndReplaceOnlyTheValueThatWasRead()
    {
        SetupStored(new Dictionary<string, string>
        {
            [Constants.BggConfig.ApiKey] = BggPlaintext,
            [Constants.ChangeDetectionConfig.ApiKey] = ChangeDetectionPlaintext
        });

        var migrated = await _migrator.ProtectLegacyKeysAsync();

        migrated.Should().Be(2);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _protectorMock.Verify(x => x.IsProtected(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.IsProtected(ChangeDetectionPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Protect(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Protect(ChangeDetectionPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Reveal(Constants.BggConfig.ApiKey, BggProtected), Times.Once);
        _protectorMock.Verify(x => x.Reveal(Constants.ChangeDetectionConfig.ApiKey, ChangeDetectionProtected), Times.Once);
        _configRepositoryMock.Verify(x => x.ReplaceConfigValueAsync(Constants.BggConfig.ApiKey, BggPlaintext, BggProtected), Times.Once);
        _configRepositoryMock.Verify(x => x.ReplaceConfigValueAsync(Constants.ChangeDetectionConfig.ApiKey, ChangeDetectionPlaintext, ChangeDetectionProtected), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProtectLegacyKeysAsync_ShouldLeaveAlreadyEncryptedAndEmptyKeysAlone()
    {
        SetupStored(new Dictionary<string, string>
        {
            [Constants.BggConfig.ApiKey] = BggProtected,
            [Constants.ChangeDetectionConfig.ApiKey] = string.Empty
        });

        var migrated = await _migrator.ProtectLegacyKeysAsync();

        migrated.Should().Be(0);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _protectorMock.Verify(x => x.IsProtected(BggProtected), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProtectLegacyKeysAsync_ShouldMigrateOnce_WhenRunAgainAfterAMigration()
    {
        var stored = new Dictionary<string, string> { [Constants.BggConfig.ApiKey] = BggPlaintext };
        _configRepositoryMock.Setup(x => x.GetAllConfigsAsync()).ReturnsAsync(() => new Dictionary<string, string>(stored));
        _configRepositoryMock
            .Setup(x => x.ReplaceConfigValueAsync(Constants.BggConfig.ApiKey, BggPlaintext, BggProtected))
            .Callback<string, string, string>((key, _, value) => stored[key] = value)
            .ReturnsAsync(true);

        var first = await _migrator.ProtectLegacyKeysAsync();
        var second = await _migrator.ProtectLegacyKeysAsync();

        first.Should().Be(1);
        second.Should().Be(0);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Exactly(2));
        _protectorMock.Verify(x => x.IsProtected(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.IsProtected(BggProtected), Times.Once);
        _protectorMock.Verify(x => x.Protect(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Reveal(Constants.BggConfig.ApiKey, BggProtected), Times.Once);
        _configRepositoryMock.Verify(x => x.ReplaceConfigValueAsync(Constants.BggConfig.ApiKey, BggPlaintext, BggProtected), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProtectLegacyKeysAsync_ShouldKeepThePlaintext_WhenTheEncryptedValueCannotBeReadBack()
    {
        SetupStored(new Dictionary<string, string> { [Constants.BggConfig.ApiKey] = BggPlaintext });
        _protectorMock.Setup(x => x.Reveal(Constants.BggConfig.ApiKey, BggProtected)).Returns((string?)null);

        var migrated = await _migrator.ProtectLegacyKeysAsync();

        migrated.Should().Be(0);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _protectorMock.Verify(x => x.IsProtected(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Protect(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Reveal(Constants.BggConfig.ApiKey, BggProtected), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProtectLegacyKeysAsync_ShouldNotCountAKeyThatChangedWhileMigrating()
    {
        SetupStored(new Dictionary<string, string> { [Constants.BggConfig.ApiKey] = BggPlaintext });
        _configRepositoryMock
            .Setup(x => x.ReplaceConfigValueAsync(Constants.BggConfig.ApiKey, BggPlaintext, BggProtected))
            .ReturnsAsync(false);

        var migrated = await _migrator.ProtectLegacyKeysAsync();

        migrated.Should().Be(0);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _protectorMock.Verify(x => x.IsProtected(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Protect(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Reveal(Constants.BggConfig.ApiKey, BggProtected), Times.Once);
        _configRepositoryMock.Verify(x => x.ReplaceConfigValueAsync(Constants.BggConfig.ApiKey, BggPlaintext, BggProtected), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProtectLegacyKeysAsync_ShouldNeverWriteTheEnvironmentOverride()
    {
        Environment.SetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName, "env-bgg-key");
        SetupStored(new Dictionary<string, string> { [Constants.BggConfig.ApiKey] = string.Empty });

        var migrated = await _migrator.ProtectLegacyKeysAsync();

        migrated.Should().Be(0);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        Environment.GetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName).Should().Be("env-bgg-key");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProtectLegacyKeysAsync_ShouldNotLogTheKeyValues()
    {
        SetupStored(new Dictionary<string, string> { [Constants.BggConfig.ApiKey] = BggPlaintext });

        await _migrator.ProtectLegacyKeysAsync();

        _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(BggPlaintext) || state.ToString()!.Contains(BggProtected)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _protectorMock.Verify(x => x.IsProtected(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Protect(BggPlaintext), Times.Once);
        _protectorMock.Verify(x => x.Reveal(Constants.BggConfig.ApiKey, BggProtected), Times.Once);
        _configRepositoryMock.Verify(x => x.ReplaceConfigValueAsync(Constants.BggConfig.ApiKey, BggPlaintext, BggProtected), Times.Once);
        VerifyNoOtherCalls();
    }
}
