using System;
using System.Collections.Generic;
using BoardGameTracker.Core.Configuration;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace BoardGameTracker.Tests.Configuration;

[Collection("EnvironmentVariables")]
public class DbConnectionProviderTests : IDisposable
{
    private static readonly string[] Keys = ["DB_HOST", "DB_USER", "DB_PASSWORD", "DB_NAME", "DB_PORT"];

    private readonly DbConnectionProvider _provider = new();
    private readonly Dictionary<string, string?> _originalEnvironmentVariables = new();

    public DbConnectionProviderTests()
    {
        foreach (var key in Keys)
        {
            _originalEnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("db.internal", "db.internal")]
    [InlineData("  db.internal  ", "db.internal")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void PostgresHost_ShouldTrimAndDefaultToEmpty(string? value, string expected)
    {
        Environment.SetEnvironmentVariable("DB_HOST", value);

        _provider.PostgresHost.Should().Be(expected);
    }

    [Theory]
    [InlineData("postgres", "postgres")]
    [InlineData("  postgres  ", "postgres")]
    [InlineData(null, "")]
    public void PostgresUser_ShouldTrimAndDefaultToEmpty(string? value, string expected)
    {
        Environment.SetEnvironmentVariable("DB_USER", value);

        _provider.PostgresUser.Should().Be(expected);
    }

    [Theory]
    [InlineData("s3cret", "s3cret")]
    [InlineData("  s3cret  ", "s3cret")]
    [InlineData(null, "")]
    public void PostgresPassword_ShouldTrimAndDefaultToEmpty(string? value, string expected)
    {
        Environment.SetEnvironmentVariable("DB_PASSWORD", value);

        _provider.PostgresPassword.Should().Be(expected);
    }

    [Theory]
    [InlineData("tracker", "tracker")]
    [InlineData("  tracker  ", "tracker")]
    [InlineData("", "boardgametracker")]
    [InlineData("   ", "boardgametracker")]
    [InlineData(null, "boardgametracker")]
    public void PostgresMainDb_ShouldFallBackToDefaultName(string? value, string expected)
    {
        Environment.SetEnvironmentVariable("DB_NAME", value);

        _provider.PostgresMainDb.Should().Be(expected);
    }

    [Theory]
    [InlineData("5433", 5433)]
    [InlineData("1", 1)]
    [InlineData("", 5432)]
    [InlineData("   ", 5432)]
    [InlineData("not-a-port", 5432)]
    [InlineData("5432.5", 5432)]
    [InlineData(null, 5432)]
    public void PostgresPort_ShouldFallBackTo5432_WhenValueIsNotAnInteger(string? value, int expected)
    {
        Environment.SetEnvironmentVariable("DB_PORT", value);

        _provider.PostgresPort.Should().Be(expected);
    }

    [Fact]
    public void GetPostgresConnectionString_ShouldUseRequestedDatabase_NotTheConfiguredMainDatabase()
    {
        Environment.SetEnvironmentVariable("DB_HOST", "db.internal");
        Environment.SetEnvironmentVariable("DB_USER", "postgres");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "s3cret");
        Environment.SetEnvironmentVariable("DB_NAME", "tracker");
        Environment.SetEnvironmentVariable("DB_PORT", "5433");

        var connectionString = _provider.GetPostgresConnectionString("other_db");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        builder.Database.Should().Be("other_db");
        builder.Host.Should().Be("db.internal");
        builder.Username.Should().Be("postgres");
        builder.Password.Should().Be("s3cret");
        builder.Port.Should().Be(5433);
    }

    [Fact]
    public void GetPostgresConnectionString_ShouldDisableEnlistAndIncludeErrorDetail()
    {
        Environment.SetEnvironmentVariable("DB_HOST", "db.internal");

        var connectionString = _provider.GetPostgresConnectionString("tracker");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        builder.Enlist.Should().BeFalse();
        builder.IncludeErrorDetail.Should().BeTrue();
    }

    [Fact]
    public void GetPostgresConnectionString_ShouldUseDefaultPort_WhenPortIsNotConfigured()
    {
        Environment.SetEnvironmentVariable("DB_HOST", "db.internal");

        var connectionString = _provider.GetPostgresConnectionString("tracker");

        new NpgsqlConnectionStringBuilder(connectionString).Port.Should().Be(5432);
    }
}
