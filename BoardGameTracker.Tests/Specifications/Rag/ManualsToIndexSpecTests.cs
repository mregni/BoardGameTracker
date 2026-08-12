using System;
using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Rag.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Rag;

public class ManualsToIndexSpecTests
{
    private static Manual CreateManual(int id, ManualIndexStatus status)
    {
        var manual = new Manual("Rules", "stored.pdf", "application/pdf", 100, 1, DateTime.UtcNow)
        {
            Id = id
        };

        switch (status)
        {
            case ManualIndexStatus.Indexing:
                manual.MarkIndexing();
                break;
            case ManualIndexStatus.Indexed:
                manual.MarkIndexed(1, DateTime.UtcNow);
                break;
            case ManualIndexStatus.Failed:
                manual.MarkFailed("error");
                break;
        }

        return manual;
    }

    [Theory]
    [InlineData(ManualIndexStatus.Pending, true)]
    [InlineData(ManualIndexStatus.Failed, true)]
    [InlineData(ManualIndexStatus.Indexing, true)]
    [InlineData(ManualIndexStatus.Indexed, false)]
    public void IsSatisfiedBy_ShouldSelectManual_WhenStatusRequiresIndexing(ManualIndexStatus status, bool expected)
    {
        var manual = CreateManual(1, status);

        new ManualsToIndexSpec().IsSatisfiedBy(manual).Should().Be(expected);
    }

    [Fact]
    public void Evaluate_ShouldExcludeIndexedManuals_WhenListContainsAllStatuses()
    {
        var manuals = new List<Manual>
        {
            CreateManual(1, ManualIndexStatus.Pending),
            CreateManual(2, ManualIndexStatus.Failed),
            CreateManual(3, ManualIndexStatus.Indexed),
            CreateManual(4, ManualIndexStatus.Indexing)
        };

        var result = new ManualsToIndexSpec().Evaluate(manuals).ToList();

        result.Select(m => m.Id).Should().BeEquivalentTo(new[] { 1, 2, 4 });
    }
}
