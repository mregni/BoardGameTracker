using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Tests.Support;

public sealed class SessionBuilder
{
    private static readonly PropertyInfo GameProperty = typeof(Session).GetProperty(nameof(Session.Game))!;
    private static readonly PropertyInfo SessionProperty = typeof(PlayerSession).GetProperty(nameof(PlayerSession.Session))!;
    private static readonly PropertyInfo PlayerProperty = typeof(PlayerSession).GetProperty(nameof(PlayerSession.Player))!;

    private readonly List<(int PlayerId, Player? Player, double? Score, bool Won, bool FirstPlay)> _players = [];
    private int _gameId = 1;
    private Game? _game;
    private DateTime _end = DateTime.UtcNow;
    private TimeSpan _duration = TimeSpan.FromHours(2);
    private string _comment = "Test session";
    private int? _id;
    private Location? _location;

    public SessionBuilder ForGame(int gameId)
    {
        _gameId = gameId;
        return this;
    }

    public SessionBuilder ForGame(Game game)
    {
        _game = game;
        _gameId = game.Id;
        return this;
    }

    public SessionBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public SessionBuilder DaysAgo(int days) => EndingAt(DateTime.UtcNow.AddDays(-days));

    public SessionBuilder EndingAt(DateTime end)
    {
        _end = end;
        return this;
    }

    public SessionBuilder Between(DateTime start, DateTime end)
    {
        _duration = end - start;
        _end = end;
        return this;
    }

    public SessionBuilder Lasting(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    public SessionBuilder LastingMinutes(int minutes) => Lasting(TimeSpan.FromMinutes(minutes));

    public SessionBuilder WithComment(string comment)
    {
        _comment = comment;
        return this;
    }

    public SessionBuilder AtLocation(Location location)
    {
        _location = location;
        return this;
    }

    public SessionBuilder WithPlayer(int playerId, double? score = null, bool won = false, bool firstPlay = false)
    {
        _players.Add((playerId, null, score, won, firstPlay));
        return this;
    }

    public SessionBuilder WithPlayer(Player player, double? score = null, bool won = false, bool firstPlay = false)
    {
        _players.Add((player.Id, player, score, won, firstPlay));
        return this;
    }

    public SessionBuilder WithPlayers(params int[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            WithPlayer(playerId);
        }

        return this;
    }

    public SessionBuilder WithWinner(int playerId, double? score = null, bool firstPlay = false) => WithPlayer(playerId, score, won: true, firstPlay: firstPlay);

    public SessionBuilder WithWinner(Player player, double? score = null, bool firstPlay = false) => WithPlayer(player, score, won: true, firstPlay: firstPlay);

    public Session Build()
    {
        var session = new Session(_gameId, _end - _duration, _end, _comment);
        if (_id.HasValue)
        {
            session.Id = _id.Value;
        }

        if (_game != null)
        {
            GameProperty.SetValue(session, _game);
        }

        if (_location != null)
        {
            session.SetLocation(_location);
        }

        foreach (var (playerId, player, score, won, firstPlay) in _players)
        {
            session.AddPlayerSession(playerId, score, firstPlay, won);
            if (player != null)
            {
                PlayerProperty.SetValue(session.PlayerSessions.Single(ps => ps.PlayerId == playerId), player);
            }
        }

        foreach (var playerSession in session.PlayerSessions)
        {
            SessionProperty.SetValue(playerSession, session);
        }

        return session;
    }
}
