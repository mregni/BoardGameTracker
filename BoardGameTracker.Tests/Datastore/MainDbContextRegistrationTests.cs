using System.Collections.Generic;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Extensions;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.Tests.Datastore;

[Collection(EnvironmentVariablesCollection.Name)]
public class MainDbContextRegistrationTests : IDisposable
{
    private static readonly string[] Keys = ["ASPNETCORE_ENVIRONMENT", "ENVIRONMENT", "EF_SENSITIVE_LOGGING", "DB_HOST"];

    private readonly Dictionary<string, string?> _originalEnvironmentVariables = [];

    public MainDbContextRegistrationTests()
    {
        foreach (var key in Keys)
        {
            _originalEnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, null);
        }

        Environment.SetEnvironmentVariable("DB_HOST", "db.internal");
    }

    public void Dispose()
    {
        foreach (var kvp in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
        GC.SuppressFinalize(this);
    }

    private static bool ResolveSensitiveDataLoggingEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreService();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<MainDbContext>>();
        return options.FindExtension<CoreOptionsExtension>()!.IsSensitiveDataLoggingEnabled;
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void AddCoreService_ShouldNotEnableSensitiveDataLogging_ByDefault(string environmentName)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);

        ResolveSensitiveDataLoggingEnabled().Should().BeFalse();
    }

    [Fact]
    public void AddCoreService_ShouldEnableSensitiveDataLogging_OnlyWithOptInFlag()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        Environment.SetEnvironmentVariable("EF_SENSITIVE_LOGGING", "true");

        ResolveSensitiveDataLoggingEnabled().Should().BeTrue();
    }
}
