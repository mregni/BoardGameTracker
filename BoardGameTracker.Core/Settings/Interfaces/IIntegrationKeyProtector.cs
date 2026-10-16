namespace BoardGameTracker.Core.Settings.Interfaces;

public interface IIntegrationKeyProtector
{
    string Protect(string plaintext);
    bool IsProtected(string stored);
    string? Reveal(string setting, string? stored);
}
