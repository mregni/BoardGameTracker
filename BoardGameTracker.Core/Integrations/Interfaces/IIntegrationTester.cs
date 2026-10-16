using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Integrations;

namespace BoardGameTracker.Core.Integrations.Interfaces;

public interface IIntegrationTester
{
    IntegrationKind Integration { get; }
    Task<IntegrationTestResult> TestAsync(CancellationToken cancellationToken = default);
}
