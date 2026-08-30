using BoardGameTracker.Common.Models.ChangeDetection;

namespace BoardGameTracker.Core.ChangeDetection.Interfaces;

public interface IChangeDetectionClient
{
    Task<ChangeDetectionResult> GetLatestAsync(
        string watchId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, ChangeDetectionResult>> GetLatestAsync(
        IReadOnlyCollection<string> watchIds,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);
}
