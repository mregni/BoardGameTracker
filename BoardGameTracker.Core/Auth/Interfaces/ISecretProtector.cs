namespace BoardGameTracker.Core.Auth.Interfaces;

public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string stored);
}
