namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IDataProtectionKeyReEncryptor
{
    Task<int> EncryptPlaintextKeysAsync();
}
