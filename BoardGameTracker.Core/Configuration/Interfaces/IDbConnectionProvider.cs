namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IDbConnectionProvider
{
    string PostgresHost { get; }
    int PostgresPort { get; }
    string PostgresUser { get; }
    string PostgresPassword { get; }
    string PostgresMainDb { get; }
    string GetPostgresConnectionString(string dbName);
}
