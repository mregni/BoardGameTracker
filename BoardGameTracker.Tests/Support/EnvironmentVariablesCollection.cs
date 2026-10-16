using Xunit;

namespace BoardGameTracker.Tests.Support;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection
{
    public const string Name = "EnvironmentVariables";
}
