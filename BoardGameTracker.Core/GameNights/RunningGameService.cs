using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.GameNights;

public class RunningGameService : IRunningGameService
{
    private readonly IRepository<RunningGame> _runningGameRepository;
    private readonly IReadRepository<GameNight> _gameNightRepository;
    private readonly IGameService _gameService;
    private readonly IPlayerService _playerService;
    private readonly ISessionService _sessionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RunningGameService> _logger;

    public RunningGameService(
        IRepository<RunningGame> runningGameRepository,
        IReadRepository<GameNight> gameNightRepository,
        IGameService gameService,
        IPlayerService playerService,
        ISessionService sessionService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<RunningGameService> logger)
    {
        _runningGameRepository = runningGameRepository;
        _gameNightRepository = gameNightRepository;
        _gameService = gameService;
        _playerService = playerService;
        _sessionService = sessionService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public Task<RunningGame?> GetById(int id)
    {
        _logger.LogDebug("Fetching running game {RunningGameId}", id);
        return _runningGameRepository.GetByIdAsync(id);
    }

    public async Task<RunningGame> Start(int gameNightId, StartRunningGameCommand command)
    {
        _logger.LogDebug("Starting game {GameId} on game night {GameNightId}", command.GameId, gameNightId);
        if (!await _gameNightRepository.AnyAsync(new GameNightByIdSpec(gameNightId)))
        {
            throw new EntityNotFoundException(nameof(GameNight), gameNightId);
        }

        if (!await _gameService.ExistsAsync(command.GameId))
        {
            throw new EntityNotFoundException(nameof(Game), command.GameId);
        }

        var playerIds = command.PlayerIds.Distinct().ToList();
        var missing = playerIds.Except(await _playerService.GetExistingIdsAsync(playerIds)).ToList();
        if (missing.Count > 0)
        {
            throw new EntityNotFoundException(nameof(Player), missing[0]);
        }

        var runningGame = RunningGame.Create(gameNightId, command.GameId, _dateTimeProvider.UtcNow, playerIds);
        await _runningGameRepository.CreateAsync(runningGame);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Running game {RunningGameId} started on game night {GameNightId}", runningGame.Id, gameNightId);

        return runningGame;
    }

    public async Task<Session> Finish(int id, FinishRunningGameCommand command)
    {
        _logger.LogDebug("Finishing running game {RunningGameId}", id);
        var runningGame = await _runningGameRepository.SingleOrDefaultAsync(new RunningGameByIdSpec(id));
        if (runningGame == null)
        {
            throw new EntityNotFoundException(nameof(RunningGame), id);
        }

        var start = command.Start ?? runningGame.Start;
        var minutes = command.Minutes ?? MinutesSince(start);

        await _runningGameRepository.DeleteAsync(id);
        var session = await _sessionService.CreateFromCommand(new CreateSessionCommand
        {
            GameId = runningGame.GameId,
            Start = start,
            Minutes = minutes,
            LocationId = runningGame.GameNight.LocationId,
            GameNightId = runningGame.GameNightId,
            PlayerSessions = command.PlayerSessions,
        });
        _logger.LogInformation("Running game {RunningGameId} finished as session {SessionId}", id, session.Id);

        return session;
    }

    private int MinutesSince(DateTime start)
    {
        var elapsed = _dateTimeProvider.UtcNow - start.ToUniversalTime();
        return Math.Max(1, (int)Math.Round(elapsed.TotalMinutes));
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Discarding running game {RunningGameId}", id);
        await _runningGameRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Running game {RunningGameId} discarded", id);
    }
}
