using System;
using System.Security.Cryptography;
using System.Xml.Linq;
using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class DataProtectionKeyEncryptionTests
{
    private const string Secret = "data-protection-secret-that-is-long-enough";
    private static readonly XElement KeyElement = XElement.Parse("<descriptor><masterKey>c2VjcmV0LWtleS1tYXRlcmlhbA==</masterKey></descriptor>");

    [Fact]
    public void EncryptAndDecrypt_ShouldRoundTripTheKeyElement_WithoutStoringItInPlainText()
    {
        var material = new DataProtectionKeyMaterial(Secret);
        var encrypted = new SecretXmlEncryptor(material).Encrypt(KeyElement);

        encrypted.EncryptedElement.ToString().Should().NotContain("c2VjcmV0LWtleS1tYXRlcmlhbA==");
        encrypted.DecryptorType.Should().Be<SecretXmlDecryptor>();

        var decrypted = Decryptor(material).Decrypt(encrypted.EncryptedElement);

        XNode.DeepEquals(decrypted, KeyElement).Should().BeTrue();
    }

    [Fact]
    public void Decrypt_ShouldFail_WithADifferentSecret()
    {
        var encrypted = new SecretXmlEncryptor(new DataProtectionKeyMaterial(Secret)).Encrypt(KeyElement);

        var act = () => Decryptor(new DataProtectionKeyMaterial(Secret + "-changed")).Decrypt(encrypted.EncryptedElement);

        act.Should().Throw<CryptographicException>();
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

    private static SecretXmlDecryptor Decryptor(DataProtectionKeyMaterial material) =>
        new(new ServiceCollection().AddSingleton(material).BuildServiceProvider());
}
