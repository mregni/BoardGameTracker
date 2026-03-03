using BoardGameTracker.Core.Configuration.Interfaces;
using Npgsql;

namespace BoardGameTracker.Core.Configuration;

public class DbConnectionProvider : IDbConnectionProvider
{
    public string PostgresHost => GetEnvValue("DB_HOST", string.Empty);
    public string PostgresUser => GetEnvValue("DB_USER", string.Empty);
    public string PostgresPassword => GetEnvValue("DB_PASSWORD", string.Empty);
    public string PostgresMainDb => GetEnvValue("DB_NAME", "boardgametracker");
    public int PostgresPort => int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var port) ? port : 5432;

    public string GetPostgresConnectionString(string dbName)
    {
        var connectionBuilder = new NpgsqlConnectionStringBuilder
        {
            Database = dbName,
            Host = PostgresHost,
            Username = PostgresUser,
            Password = PostgresPassword,
            Port = PostgresPort,
            Enlist = false,
            IncludeErrorDetail = true
        };

        return connectionBuilder.ConnectionString;
    }

    private static string GetEnvValue(string key, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrWhiteSpace(value) ? value.Trim() : defaultValue;
    }
}
