using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Common.Models.Integrations;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Core.Integrations.Interfaces;

namespace BoardGameTracker.Core.Integrations;

public class ChangeDetectionConnectionTester : IIntegrationTester
{
    private readonly IChangeDetectionClient _changeDetectionClient;

    public ChangeDetectionConnectionTester(IChangeDetectionClient changeDetectionClient)
    {
        _changeDetectionClient = changeDetectionClient;
    }

    public IntegrationKind Integration => IntegrationKind.ChangeDetection;

    public async Task<IntegrationTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var (status, _) = await _changeDetectionClient.TestConnectionAsync(cancellationToken);
        return status switch
        {
            ChangeDetectionStatus.Ok => IntegrationTestResult.Ok,
            ChangeDetectionStatus.NotConfigured => IntegrationTestResult.NotConfigured,
            ChangeDetectionStatus.Unauthorized => IntegrationTestResult.Unauthorized,
            ChangeDetectionStatus.Misconfigured => IntegrationTestResult.Unreachable(IntegrationMessageKeys.Misconfigured),
            _ => IntegrationTestResult.Unreachable()
        };
    }
}
