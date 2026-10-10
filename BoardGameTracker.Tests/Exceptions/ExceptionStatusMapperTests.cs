using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BoardGameTracker.Tests.Exceptions;

public class ExceptionStatusMapperTests
{
    public static TheoryData<Exception, bool> Reporting => new()
    {
        { new InvalidOperationException("bug"), true },
        { new NullReferenceException(), true },
        { new TaskCanceledException("timed out", new TimeoutException()), true },
        { new OperationCanceledException(), false },
        { new TaskCanceledException(), false },
        { new ValidationException("error.x"), false },
        { new DomainException("error.x"), false },
        { new EntityNotFoundException("Game", 1), false },
        { new AuthenticationFailedException("error.x"), false },
        { new ArgumentOutOfRangeException("year"), false },
        { new KeyNotFoundException(), false },
        { new DbUpdateConcurrencyException(), false },
        { new DbUpdateException("save", new PostgresException("duplicate", "ERROR", "ERROR", "23505")), false },
        { new DbUpdateException("save", new PostgresException("disk full", "ERROR", "ERROR", "53100")), true },
        { new BggFeatureDisabledException(), false },
        { new ConfigMissingException("BGG"), false },
        { new FeatureDisabledException("shelf_of_shame_enabled"), false },
    };

    [Theory]
    [MemberData(nameof(Reporting))]
    public void ShouldReport_ShouldOnlyReportWhatTheApiAnswersWithAServerError(Exception exception, bool expected)
    {
        ExceptionStatusMapper.ShouldReport(exception).Should().Be(expected);
    }

    [Fact]
    public void Map_ShouldTurnAConstraintViolationIntoABadRequest()
    {
        var (status, _) = ExceptionStatusMapper.Map(new DbUpdateException("save", new PostgresException("duplicate", "ERROR", "ERROR", "23505")));

        status.Should().Be(400);
    }

    [Fact]
    public void Map_ShouldTurnADisabledFeatureIntoANotFoundWithTheTranslationKey()
    {
        var (status, message) = ExceptionStatusMapper.Map(new FeatureDisabledException("game_nights_enabled"));

        status.Should().Be(404);
        message.Should().Be(BoardGameTracker.Common.Constants.Errors.FeatureDisabled);
    }
}
