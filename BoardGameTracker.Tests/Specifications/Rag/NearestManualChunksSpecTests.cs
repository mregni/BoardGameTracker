using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Rag.Specifications;
using FluentAssertions;
using Pgvector;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Rag;

public class NearestManualChunksSpecTests
{
    private static ManualChunk CreateChunk(int manualId, int gameId) =>
        new(manualId, gameId, 0, "content", 1, new Vector(new float[1024]));

    private static Vector QueryVector() => new(new float[1024]);

    [Fact]
    public void IsSatisfiedBy_ShouldMatchOnlyChunksOfRequestedGame_WhenManualIdIsNull()
    {
        var spec = new NearestManualChunksSpec(1, QueryVector(), 5);

        spec.IsSatisfiedBy(CreateChunk(10, 1)).Should().BeTrue();
        spec.IsSatisfiedBy(CreateChunk(10, 2)).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldMatchChunksFromAnyManual_WhenManualIdIsNull()
    {
        var spec = new NearestManualChunksSpec(1, QueryVector(), 5);

        spec.IsSatisfiedBy(CreateChunk(10, 1)).Should().BeTrue();
        spec.IsSatisfiedBy(CreateChunk(11, 1)).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldMatchOnlyRequestedManual_WhenManualIdIsSupplied()
    {
        var spec = new NearestManualChunksSpec(1, QueryVector(), 5, 10);

        spec.IsSatisfiedBy(CreateChunk(10, 1)).Should().BeTrue();
        spec.IsSatisfiedBy(CreateChunk(11, 1)).Should().BeFalse();
        spec.IsSatisfiedBy(CreateChunk(10, 2)).Should().BeFalse();
    }

    [Fact]
    public void Take_ShouldEqualRequestedK_WhenSpecIsConstructed()
    {
        var spec = new NearestManualChunksSpec(1, QueryVector(), 7);

        spec.Take.Should().Be(7);
    }
}
