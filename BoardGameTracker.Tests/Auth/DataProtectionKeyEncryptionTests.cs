using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Linq;
using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class DataProtectionKeyEncryptionTests
{
    private static readonly string Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private const string Payload = "oidc-client-secret-value";
    private static readonly string MasterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static readonly XElement KeyElement = XElement.Parse($"<descriptor><masterKey>{MasterKey}</masterKey></descriptor>");

    private readonly Mock<ILogger> _loggerMock = new();

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void EncryptAndDecrypt_ShouldRoundTripTheKeyElement_WithoutStoringItInPlainText()
    {
        var material = new DataProtectionKeyMaterial(Secret);
        var encrypted = new SecretXmlEncryptor(material).Encrypt(KeyElement);

        encrypted.EncryptedElement.ToString().Should().NotContain(MasterKey);
        encrypted.DecryptorType.Should().Be<SecretXmlDecryptor>();

        var decrypted = Decryptor(material).Decrypt(encrypted.EncryptedElement);

        XNode.DeepEquals(decrypted, KeyElement).Should().BeTrue();
    }

    [Fact]
    public void Encrypt_ShouldUseAFreshNonce_ForEveryCall()
    {
        var encryptor = new SecretXmlEncryptor(new DataProtectionKeyMaterial(Secret));

        var first = encryptor.Encrypt(KeyElement).EncryptedElement.ToString();
        var second = encryptor.Encrypt(KeyElement).EncryptedElement.ToString();

        first.Should().NotBe(second);
    }

    [Fact]
    public void Decrypt_ShouldFail_WithADifferentSecret()
    {
        var encrypted = new SecretXmlEncryptor(new DataProtectionKeyMaterial(Secret)).Encrypt(KeyElement);

        var act = () => Decryptor(new DataProtectionKeyMaterial(Secret + "-changed")).Decrypt(encrypted.EncryptedElement);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_ShouldFail_WhenThePayloadWasTamperedWith()
    {
        var material = new DataProtectionKeyMaterial(Secret);
        var encrypted = new SecretXmlEncryptor(material).Encrypt(KeyElement).EncryptedElement;
        var payload = Convert.FromBase64String((string)encrypted.Element("value")!);
        payload[^1] ^= 0xFF;
        encrypted.Element("value")!.Value = Convert.ToBase64String(payload);

        var act = () => Decryptor(material).Decrypt(encrypted);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_ShouldFail_WhenThePayloadIsTooShort()
    {
        var element = new XElement("encryptedKey", new XElement("value", Convert.ToBase64String(new byte[8])));

        var act = () => Decryptor(new DataProtectionKeyMaterial(Secret)).Decrypt(element);

        act.Should().Throw<CryptographicException>().WithMessage("*malformed*");
    }

    [Fact]
    public void Decrypt_ShouldFail_WhenTheVariableIsMissing()
    {
        var encrypted = new SecretXmlEncryptor(new DataProtectionKeyMaterial(Secret)).Encrypt(KeyElement);

        var act = () => new SecretXmlDecryptor(new ServiceCollection().BuildServiceProvider()).Decrypt(encrypted.EncryptedElement);

        act.Should().Throw<CryptographicException>().WithMessage("*DATA_PROTECTION_KEY*");
    }

    [Fact]
    public void KeyMaterial_ShouldRejectAShortSecret()
    {
        var act = () => new DataProtectionKeyMaterial("too-short");

        act.Should().Throw<ArgumentException>().WithMessage("*at least 32*");
    }

    [Theory]
    [InlineData("CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS")]
    [InlineData("changeme_generate_at_least_32_characters")]
    public void KeyMaterial_ShouldRejectThePlaceholder(string secret)
    {
        var act = () => new DataProtectionKeyMaterial(secret);

        act.Should().Throw<ArgumentException>().WithMessage("DATA_PROTECTION_KEY still holds the CHANGEME placeholder*");
    }

    [Fact]
    public void ProtectKeysWithSecret_ShouldFail_WhenTheSecretIsThePlaceholder()
    {
        var act = () => new ServiceCollection().AddDataProtection().ProtectKeysWithSecret("CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS");

        act.Should().Throw<ArgumentException>().WithMessage("*DATA_PROTECTION_KEY*CHANGEME*");
    }

    [Fact]
    public void KeyMaterial_ShouldDeriveTheSameKey_FromTheSameSecret()
    {
        new DataProtectionKeyMaterial(Secret).Key.Should().Equal(new DataProtectionKeyMaterial(Secret).Key);
        new DataProtectionKeyMaterial(Secret).Key.Should().NotEqual(new DataProtectionKeyMaterial(Secret + "-changed").Key);
    }

    [Fact]
    public void ProtectKeysWithSecret_ShouldRegisterTheEncryptor_WhenTheSecretIsSet()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().ProtectKeysWithSecret(Secret);
        using var provider = services.BuildServiceProvider();

        provider.GetService<DataProtectionKeyMaterial>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlEncryptor.Should().BeOfType<SecretXmlEncryptor>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProtectKeysWithSecret_ShouldLeaveTheKeysUnencrypted_WhenTheSecretIsNotSet(string? secret)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().ProtectKeysWithSecret(secret);
        using var provider = services.BuildServiceProvider();

        provider.GetService<DataProtectionKeyMaterial>().Should().BeNull();
        provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlEncryptor.Should().BeNull();
    }

    [Fact]
    public void ProtectKeysWithSecret_ShouldFail_WhenTheSecretIsTooShort()
    {
        var act = () => new ServiceCollection().AddDataProtection().ProtectKeysWithSecret("too-short");

        act.Should().Throw<ArgumentException>().WithMessage("*DATA_PROTECTION_KEY*");
    }

    [Fact]
    public void WarnWhenKeysAreUnencrypted_ShouldLogOneWarning_WhenTheSecretIsNotSet()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().ProtectKeysWithSecret(null);
        using var provider = services.BuildServiceProvider();

        var warned = _loggerMock.Object.WarnWhenKeysAreUnencrypted(provider);

        warned.Should().BeTrue();
        _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("DATA_PROTECTION_KEY is not set") && state.ToString()!.Contains("unencrypted")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void WarnWhenKeysAreUnencrypted_ShouldStaySilent_WhenTheSecretIsSet()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().ProtectKeysWithSecret(Secret);
        using var provider = services.BuildServiceProvider();

        var warned = _loggerMock.Object.WarnWhenKeysAreUnencrypted(provider);

        warned.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ProtectedData_ShouldStayReadable_WhenItsPlaintextKeyIsReEncrypted()
    {
        var repository = new MemoryXmlRepository();
        var protectedPayload = CreateProtector(repository, null).Protect(Payload);
        repository.Elements.Should().ContainSingle().Which.ToString().Should().Contain("<masterKey");

        var original = repository.Elements[0].ToString(SaveOptions.DisableFormatting);
        var reEncrypted = DataProtectionKeyXml.EncryptPlaintextSecrets(original, new DataProtectionKeyMaterial(Secret));
        repository.Elements[0] = XElement.Parse(reEncrypted!);

        reEncrypted.Should().NotContain("<masterKey").And.Contain("encryptedSecret").And.Contain(nameof(SecretXmlDecryptor));
        var frameworkKey = new MemoryXmlRepository();
        CreateProtector(frameworkKey, Secret).Protect(Payload);
        EncryptedSecretName(repository.Elements[0]).Should().Be(EncryptedSecretName(frameworkKey.Elements[0]));
        CreateProtector(repository, Secret).Unprotect(protectedPayload).Should().Be(Payload);
        var withoutSecret = () => CreateProtector(repository, null).Unprotect(protectedPayload);
        withoutSecret.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void NewKeys_ShouldBeStoredEncrypted_AndNeedTheSameSecretToBeRead()
    {
        var repository = new MemoryXmlRepository();
        var protectedPayload = CreateProtector(repository, Secret).Protect(Payload);

        repository.Elements.Should().ContainSingle().Which.ToString().Should().NotContain("<masterKey").And.Contain("encryptedSecret");
        DataProtectionKeyXml.EncryptPlaintextSecrets(repository.Elements[0].ToString(), new DataProtectionKeyMaterial(Secret)).Should().BeNull();
        CreateProtector(repository, Secret).Unprotect(protectedPayload).Should().Be(Payload);
        var withOtherSecret = () => CreateProtector(repository, Secret + "-changed").Unprotect(protectedPayload);
        withOtherSecret.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void EncryptPlaintextSecrets_ShouldReturnNull_WhenNothingRequiresEncryption()
    {
        DataProtectionKeyXml.EncryptPlaintextSecrets("<key><descriptor /></key>", new DataProtectionKeyMaterial(Secret)).Should().BeNull();
    }

    private static IDataProtector CreateProtector(IXmlRepository repository, string? secret)
    {
        var services = new ServiceCollection();
        services.AddDataProtection()
            .SetApplicationName("boardgametracker-tests")
            .AddKeyManagementOptions(options => options.XmlRepository = repository)
            .ProtectKeysWithSecret(secret);
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>().CreateProtector("tests");
    }

    private static XName EncryptedSecretName(XElement key) =>
        key.Descendants().Single(element => element.Name.LocalName == "encryptedSecret").Name;

    private static SecretXmlDecryptor Decryptor(DataProtectionKeyMaterial material) =>
        new(new ServiceCollection().AddSingleton(material).BuildServiceProvider());

    private sealed class MemoryXmlRepository : IXmlRepository
    {
        public List<XElement> Elements { get; } = [];

        public IReadOnlyCollection<XElement> GetAllElements() => Elements.Select(element => new XElement(element)).ToList();

        public void StoreElement(XElement element, string friendlyName) => Elements.Add(new XElement(element));
    }
}
