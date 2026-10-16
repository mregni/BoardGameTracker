using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Npgsql;
using Xunit;

namespace BoardGameTracker.Tests.Datastore;

public class MainDbContextOptionsTests : IDisposable
{
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock = new();
    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create("Host=db.internal;Database=tracker");

    public void Dispose()
    {
        _dataSource.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    private bool BuildSensitiveDataLoggingEnabled()
    {
        var builder = new DbContextOptionsBuilder<MainDbContext>();
        MainDbContextOptions.Configure(builder, _dataSource, _environmentProviderMock.Object);
        return builder.Options.FindExtension<CoreOptionsExtension>()!.IsSensitiveDataLoggingEnabled;
    }

    [Fact]
    public void Configure_ShouldNotEnableSensitiveDataLogging_InDevelopmentWithoutOptIn()
    {
        _environmentProviderMock.Setup(x => x.IsDevelopment).Returns(true);
        _environmentProviderMock.Setup(x => x.EfSensitiveDataLogging).Returns(false);

        var enabled = BuildSensitiveDataLoggingEnabled();

        enabled.Should().BeFalse();
        _environmentProviderMock.Verify(x => x.EfSensitiveDataLogging, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Configure_ShouldNotEnableSensitiveDataLogging_InProductionWithoutOptIn()
    {
        _environmentProviderMock.Setup(x => x.EfSensitiveDataLogging).Returns(false);

        var enabled = BuildSensitiveDataLoggingEnabled();

        enabled.Should().BeFalse();
        _environmentProviderMock.Verify(x => x.EfSensitiveDataLogging, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Configure_ShouldEnableSensitiveDataLogging_WhenOptedIn()
    {
        _environmentProviderMock.Setup(x => x.EfSensitiveDataLogging).Returns(true);

        var enabled = BuildSensitiveDataLoggingEnabled();

        enabled.Should().BeTrue();
        _environmentProviderMock.Verify(x => x.EfSensitiveDataLogging, Times.Once);
        VerifyNoOtherCalls();
    }
}
