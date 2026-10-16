using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Integrations.Interfaces;

public interface IIntegrationStatusService
{
    Task<IReadOnlyList<IntegrationStatusDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IntegrationStatusDto> TestAsync(IntegrationKind integration, CancellationToken cancellationToken = default);
    void Invalidate();
}
