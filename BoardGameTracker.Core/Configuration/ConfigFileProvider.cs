using BoardGameTracker.Core.Commands;
using BoardGameTracker.Core.Configuration.Interfaces;

namespace BoardGameTracker.Core.Configuration;

public class ConfigFileProvider : IConfigFileProvider
{
    public Task Handle(ApplicationStartedCommand request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}