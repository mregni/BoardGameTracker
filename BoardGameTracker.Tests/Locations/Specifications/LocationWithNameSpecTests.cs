using System.Linq.Expressions;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Locations.Specifications;

public class LocationWithNameSpecTests
{
    public static bool Matches(LocationWithNameSpec spec, string pattern, int? excludedId)
    {
        var excludes = ExcludesFilter(spec);
        return Pattern(spec) == pattern
            && excludes(new Location("Home") { Id = (excludedId ?? 1) + 1 })
            && excludes(new Location("Home") { Id = excludedId ?? 1 }) == (excludedId == null);
    }

    private static string Pattern(LocationWithNameSpec spec)
    {
        var like = (MethodCallExpression)spec.WhereExpressions.First().Filter.Body;
        return (string)Expression.Lambda(like.Arguments[2]).Compile().DynamicInvoke()!;
    }

    private static Func<Location, bool> ExcludesFilter(LocationWithNameSpec spec)
    {
        return spec.WhereExpressions.Last().Filter.Compile();
    }

    [Theory]
    [InlineData("Attic", "Attic")]
    [InlineData("  Attic  ", "Attic")]
    [InlineData("100%", "100\\%")]
    [InlineData("a_b", "a\\_b")]
    [InlineData("a\\b", "a\\\\b")]
    public void Spec_ShouldEscapeLikeCharactersInTheTrimmedName(string name, string expected)
    {
        Pattern(new LocationWithNameSpec(name)).Should().Be(expected);
    }

    [Fact]
    public void Spec_ShouldMatchEveryLocation_WhenNoLocationIsExcluded()
    {
        var excludes = ExcludesFilter(new LocationWithNameSpec("Attic"));

        excludes(new Location("Attic") { Id = 1 }).Should().BeTrue();
    }

    [Fact]
    public void Spec_ShouldSkipOnlyTheExcludedLocation()
    {
        var excludes = ExcludesFilter(new LocationWithNameSpec("Attic", 4));

        excludes(new Location("Attic") { Id = 4 }).Should().BeFalse();
        excludes(new Location("Attic") { Id = 5 }).Should().BeTrue();
    }
}
