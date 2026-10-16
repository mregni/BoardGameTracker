using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class DataProtectionKeyReEncryptorTests : IDisposable
{
    private const string Secret = "data-protection-secret-that-is-long-enough";
    private const string MasterKey = "c2VjcmV0LWtleS1tYXRlcmlhbA==";
    private const string PlaintextKey =
        "<key id=\"1\" version=\"1\"><descriptor><descriptor><masterKey p4:requiresEncryption=\"true\" xmlns:p4=\"http://schemas.asp.net/2015/03/dataProtection\"><value>" +
        MasterKey + "</value></masterKey></descriptor></descriptor></key>";

    private readonly MainDbContext _context;
    private readonly Mock<ILogger<DataProtectionKeyReEncryptor>> _loggerMock = new();

    public DataProtectionKeyReEncryptorTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
    }

    private async Task SeedAsync(params string[] xml)
    {
        foreach (var value in xml)
        {
            _context.DataProtectionKeys.Add(new DataProtectionKey { FriendlyName = $"key-{Guid.NewGuid():N}", Xml = value });
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task<string[]> StoredXmlAsync() =>
        await _context.DataProtectionKeys.AsNoTracking().OrderBy(key => key.Id).Select(key => key.Xml!).ToArrayAsync();

    private void VerifyLogged(LogLevel level, string text, Times times) =>
        _loggerMock.Verify(x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(text)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    [Fact]
    public async Task EncryptPlaintextKeysAsync_ShouldDoNothing_WhenTheSecretIsNotSet()
    {
        await SeedAsync(PlaintextKey);
        var reEncryptor = new DataProtectionKeyReEncryptor(_context, _loggerMock.Object);

        var count = await reEncryptor.EncryptPlaintextKeysAsync();

        count.Should().Be(0);
        (await StoredXmlAsync()).Should().Equal(PlaintextKey);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EncryptPlaintextKeysAsync_ShouldEncryptPlaintextKeys_AndLeaveEncryptedKeysAlone()
    {
        var material = new DataProtectionKeyMaterial(Secret);
        var alreadyEncrypted = DataProtectionKeyXml.EncryptPlaintextSecrets(PlaintextKey, material)!;
        await SeedAsync(PlaintextKey, alreadyEncrypted);
        var reEncryptor = new DataProtectionKeyReEncryptor(_context, _loggerMock.Object, material);

        var first = await reEncryptor.EncryptPlaintextKeysAsync();
        var second = await reEncryptor.EncryptPlaintextKeysAsync();

        first.Should().Be(1);
        second.Should().Be(0);
        var stored = await StoredXmlAsync();
        stored[0].Should().NotContain(MasterKey).And.NotContain("<masterKey").And.Contain("encryptedSecret");
        stored[1].Should().Be(alreadyEncrypted);
        VerifyLogged(LogLevel.Information, "Encrypted 1 existing data protection keys", Times.Once());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EncryptPlaintextKeysAsync_ShouldSkipAKeyThatCannotBeParsed_AndEncryptTheOthers()
    {
        await SeedAsync("<key><broken", PlaintextKey);
        var reEncryptor = new DataProtectionKeyReEncryptor(_context, _loggerMock.Object, new DataProtectionKeyMaterial(Secret));

        var count = await reEncryptor.EncryptPlaintextKeysAsync();

        count.Should().Be(1);
        var stored = await StoredXmlAsync();
        stored[0].Should().Be("<key><broken");
        stored[1].Should().NotContain(MasterKey);
        _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("left unencrypted")),
                It.IsAny<XmlException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        VerifyLogged(LogLevel.Information, "Encrypted 1 existing data protection keys", Times.Once());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EncryptPlaintextKeysAsync_ShouldNotLogTheKeyMaterial()
    {
        await SeedAsync(PlaintextKey);
        var reEncryptor = new DataProtectionKeyReEncryptor(_context, _loggerMock.Object, new DataProtectionKeyMaterial(Secret));

        await reEncryptor.EncryptPlaintextKeysAsync();

        VerifyLogged(LogLevel.Information, "Encrypted 1 existing data protection keys", Times.Once());
        _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(MasterKey) || state.ToString()!.Contains(Secret)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        VerifyNoOtherCalls();
    }
}
