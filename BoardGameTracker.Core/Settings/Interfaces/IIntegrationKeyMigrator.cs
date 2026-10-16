namespace BoardGameTracker.Core.Settings.Interfaces;

public interface IIntegrationKeyMigrator
{
    Task<int> ProtectLegacyKeysAsync();
}
