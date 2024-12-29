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
    string DateFormat { get; set; }
    string TimeFormat { get; set; }
    string UILanguage { get; set; }
    string Currency { get; set; }
    string DecimalSeparator { get; set; }

    string GetPostgresConnectionString(string dbName);
}