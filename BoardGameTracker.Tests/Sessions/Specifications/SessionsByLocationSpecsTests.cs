using System;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Sessions.Specifications;

public class SessionsByLocationSpecsTests
{
    private static readonly Location Home = new("Home") { Id = 1 };
    private static readonly Location Office = new("Office") { Id = 2 };

    private static Session SessionAt(int id, Location location, DateTime end)
    {
        return new SessionBuilder().WithId(id).AtLocation(location).EndingAt(end).Build();
    }

    [Fact]
    public void SessionsByLocationRecentFirstSpec_ShouldFilterByLocation_NewestFirst_AndIncludePlayers()
    {
        var older = SessionAt(1, Home, new DateTime(2030, 1, 1));
        var newer = SessionAt(2, Home, new DateTime(2030, 1, 5));
        var office = SessionAt(3, Office, new DateTime(2030, 1, 9));
        var nowhere = new SessionBuilder().WithId(4).Build();
        var spec = new SessionsByLocationRecentFirstSpec(1);

        var result = spec.Evaluate([older, newer, office, nowhere]).ToList();

        result.Select(x => x.Id).Should().Equal(2, 1);
        spec.AsNoTracking.Should().BeTrue();
        spec.ShouldIncludeExactly("PlayerSessions");
    }
}
