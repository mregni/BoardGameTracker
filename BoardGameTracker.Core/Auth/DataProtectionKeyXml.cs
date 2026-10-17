using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace BoardGameTracker.Core.Auth;

public static class DataProtectionKeyXml
{
    private static readonly XNamespace DataProtectionNamespace = "http://schemas.asp.net/2015/03/dataProtection";
    private static readonly XName RequiresEncryptionAttribute = DataProtectionNamespace + "requiresEncryption";
    private static readonly XName EncryptedSecretElement = DataProtectionNamespace + "encryptedSecret";

    public static string? EncryptPlaintextSecrets(string xml, DataProtectionKeyMaterial material)
    {
        var root = XElement.Parse(xml);
        var pending = root.DescendantsAndSelf()
            .Where(element => RequiresEncryption(element) && !element.Ancestors().Any(RequiresEncryption))
            .ToList();
        if (pending.Count == 0)
        {
            return null;
        }

        var encryptor = new SecretXmlEncryptor(material);
        foreach (var element in pending)
        {
            var info = encryptor.Encrypt(new XElement(element));
            if (!XNode.DeepEquals(SecretXmlEncryptor.Decrypt(info.EncryptedElement, material.Key), element))
            {
                throw new CryptographicException("The re-encrypted data protection key could not be read back.");
            }

            element.ReplaceWith(new XElement(
                EncryptedSecretElement,
                new XAttribute("decryptorType", info.DecryptorType.AssemblyQualifiedName!),
                info.EncryptedElement));
        }

        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static bool RequiresEncryption(XElement element)
    {
        var attribute = element.Attribute(RequiresEncryptionAttribute);
        return attribute != null && XmlConvert.ToBoolean(attribute.Value);
    }
}
