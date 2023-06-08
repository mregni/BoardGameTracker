using BoardGameTracker.Core.Commands;
using MediatR;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IConfigFileProvider: IRequestHandler<ApplicationStartedCommand>
{
    string PostgresHost { get; }
    int PostgresPort { get; }
    string PostgresUser { get; }
    string PostgresPassword { get; }
    string PostgresMainDb { get; }
}