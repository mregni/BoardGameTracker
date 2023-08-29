using BoardGameTracker.Core.Commands;
using MediatR;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IConfigFileProvider : IRequestHandler<ApplicationStartedCommand>
{
    string PostgresHost { get; }
    int PostgresPort { get; }
    string PostgresUser { get; }
    string PostgresPassword { get; }
    string PostgresMainDb { get; }
    string TimeZone { get; }
    string DateFormat { get; }
    string DateTimeFormat { get; }
    string UILanguage { get; }
    string Currency { get; }

    string GetPostgresConnectionString(string dbName);
}