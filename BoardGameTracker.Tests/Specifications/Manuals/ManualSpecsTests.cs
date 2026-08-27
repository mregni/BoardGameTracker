using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Manuals.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Manuals;

public class ManualSpecsTests
{
    private static Manual CreateManual(int id, int gameId, DateTime uploadDate) =>
        new($"Manual {id}", $"stored-{id}.pdf", "application/pdf", 100, gameId, uploadDate)
        {
            Id = id
        };

    private static List<Manual> Fixture() =>
    [
        CreateManual(1, 1, new DateTime(2030, 3, 1)),
        CreateManual(2, 1, new DateTime(2030, 1, 1)),
        CreateManual(3, 2, new DateTime(2030, 2, 1)),
        CreateManual(4, 3, new DateTime(2030, 4, 1))
    ];

    [Fact]
    public void ManualsByGameIdSpec_ShouldReturnOnlyManualsOfRequestedGame_OrderedByUploadDate()
    {
        var result = new ManualsByGameIdSpec(1).Evaluate(Fixture()).ToList();

        result.Select(x => x.Id).Should().Equal(2, 1);
    }

    [Fact]
    public void ManualsByGameIdSpec_ShouldReturnNothing_WhenGameHasNoManuals()
    {
        new ManualsByGameIdSpec(99).Evaluate(Fixture()).Should().BeEmpty();
    }

    [Fact]
    public void ManualsByGameIdsSpec_ShouldReturnOnlyManualsOfRequestedGames_OrderedByUploadDate()
    {
        var result = new ManualsByGameIdsSpec(new[] { 1, 2 }).Evaluate(Fixture()).ToList();

        result.Select(x => x.Id).Should().Equal(2, 3, 1);
    }

    [Fact]
    public void ManualsByGameIdsSpec_ShouldReturnNothing_WhenIdsAreEmpty()
    {
        new ManualsByGameIdsSpec(Array.Empty<int>()).Evaluate(Fixture()).Should().BeEmpty();
    }
}
