using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Core.Extensions;
using BoardGameTracker.Core.Integrations.Interfaces;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Npgsql;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

[Collection(EnvironmentVariablesCollection.Name)]
public class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly string? _originalRagEnabled = Environment.GetEnvironmentVariable("RAG_ENABLED");
    private readonly Mock<IDbConnectionProvider> _connectionProviderMock = new();

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("RAG_ENABLED", _originalRagEnabled);
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _connectionProviderMock.VerifyNoOtherCalls();
    }

    private ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreService();
        services.RemoveAll<IDbConnectionProvider>();
        services.AddSingleton(_connectionProviderMock.Object);
        _connectionProviderMock.Setup(x => x.PostgresMainDb).Returns("main");
        _connectionProviderMock.Setup(x => x.GetPostgresConnectionString("main")).Returns("Host=db.internal;Database=main");
        return services;
    }

    private void VerifyConnectionStringRead()
    {
        _connectionProviderMock.Verify(x => x.PostgresMainDb, Times.Once);
        _connectionProviderMock.Verify(x => x.GetPostgresConnectionString("main"), Times.Once);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("yes", false)]
    [InlineData(null, false)]
    public void AddCoreService_ShouldOnlyRunTheAiBackgroundServices_WhenRagIsEnabled(string? value, bool expected)
    {
        Environment.SetEnvironmentVariable("RAG_ENABLED", value);

        var hosted = Services().Where(x => x.ServiceType == typeof(IHostedService)).Select(x => x.ImplementationType).ToList();

        hosted.Contains(typeof(ModelProvisioningBackgroundService)).Should().Be(expected);
        hosted.Contains(typeof(ManualIndexingBackgroundService)).Should().Be(expected);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void AddCoreService_ShouldAlwaysRunTheAuthBackgroundServices()
    {
        var hosted = Services().Where(x => x.ServiceType == typeof(IHostedService)).Select(x => x.ImplementationType).ToList();

        hosted.Should().Contain(typeof(RefreshTokenCleanupService));
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(typeof(ILoginAttemptTracker), ServiceLifetime.Singleton)]
    [InlineData(typeof(IBackgroundEmailSender), ServiceLifetime.Singleton)]
    [InlineData(typeof(IEnvironmentProvider), ServiceLifetime.Singleton)]
    [InlineData(typeof(IIntegrationKeyProtector), ServiceLifetime.Singleton)]
    [InlineData(typeof(IProfileImageTicketService), ServiceLifetime.Singleton)]
    [InlineData(typeof(IUnitOfWork), ServiceLifetime.Scoped)]
    [InlineData(typeof(ISettingsService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IRepository<>), ServiceLifetime.Scoped)]
    [InlineData(typeof(IReadRepository<>), ServiceLifetime.Scoped)]
    public void AddCoreService_ShouldRegisterStatefulServicesWithTheRightLifetime(Type service, ServiceLifetime lifetime)
    {
        Services().Should().ContainSingle(x => x.ServiceType == service).Which.Lifetime.Should().Be(lifetime);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void AddCoreService_ShouldRegisterEveryBadgeEvaluatorAndIntegrationTester()
    {
        var services = Services();

        services.Count(x => x.ServiceType == typeof(IBadgeEvaluator)).Should().Be(15);
        services.Where(x => x.ServiceType == typeof(IBadgeEvaluator)).Select(x => x.ImplementationType).Should().OnlyHaveUniqueItems();
        services.Count(x => x.ServiceType == typeof(IIntegrationTester)).Should().Be(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DataSource_ShouldUseTheConnectionStringOfTheMainDatabase()
    {
        using var provider = Services().BuildServiceProvider();

        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();

        new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).Host.Should().Be("db.internal");
        new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).Database.Should().Be("main");
        VerifyConnectionStringRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DataSource_ShouldFailClearly_WhenTheConnectionProviderIsMissing()
    {
        var services = Services();
        services.RemoveAll<IDbConnectionProvider>();
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<NpgsqlDataSource>();

        act.Should().Throw<ServiceNotResolvedException>().WithMessage("dbConnectionProvider could not be resolved");
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DbContext_ShouldFailClearly_WhenTheDataSourceIsMissing()
    {
        var services = Services();
        services.RemoveAll<NpgsqlDataSource>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<DbContextOptions<MainDbContext>>();

        act.Should().Throw<ServiceNotResolvedException>().WithMessage("NpgsqlDataSource could not be resolved");
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DbContext_ShouldFailClearly_WhenTheEnvironmentProviderIsMissing()
    {
        var services = Services();
        services.RemoveAll<IEnvironmentProvider>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<DbContextOptions<MainDbContext>>();

        act.Should().Throw<ServiceNotResolvedException>().WithMessage("environmentProvider could not be resolved");
        VerifyConnectionStringRead();
        VerifyNoOtherCalls();
    }
}
