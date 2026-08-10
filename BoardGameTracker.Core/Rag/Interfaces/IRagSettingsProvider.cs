namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IRagSettingsProvider
{
    Task<RagSettings> GetAsync();
}
