using System.Collections.Generic;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class ListExtensionsTests
{
    [Fact]
    public void AddIfNotNull_ShouldAddItem_WhenItemIsNotNull()
    {
        var list = new List<string>();
        const string item = "test item";

        list.AddIfNotNull(item);

        list.Should().HaveCount(1);
        list.Should().Contain(item);
    }

    [Fact]
    public void AddIfNotNull_ShouldNotAddItem_WhenItemIsNull()
    {
        var list = new List<string>();
        string? item = null;

        list.AddIfNotNull(item);

        list.Should().BeEmpty();
    }

    [Fact]
    public void AddIfNotNull_ShouldAddItemToExistingList_WhenItemIsNotNull()
    {
        var list = new List<string> {"existing item"};
        const string item = "new item";

        list.AddIfNotNull(item);

        list.Should().HaveCount(2);
        list.Should().Contain("existing item");
        list.Should().Contain("new item");
    }

    [Fact]
    public void AddIfNotNull_ShouldNotChangeExistingList_WhenItemIsNull()
    {
        var list = new List<string> {"existing item"};
        string? item = null;

        list.AddIfNotNull(item);

        list.Should().HaveCount(1);
        list.Should().Contain("existing item");
    }

    [Fact]
    public void AddIfNotNull_ShouldAddMultipleItems_WhenCalledMultipleTimes()
    {
        var list = new List<string>();

        list.AddIfNotNull("item1");
        list.AddIfNotNull("item2");
        list.AddIfNotNull(null);
        list.AddIfNotNull("item3");

        list.Should().HaveCount(3);
        list.Should().Equal("item1", "item2", "item3");
    }

    [Fact]
    public void AddIfNotNull_ShouldWorkWithIntegerType_WhenItemIsNotNull()
    {
        var list = new List<int?>();
        int? item = 42;

        list.AddIfNotNull(item);

        list.Should().HaveCount(1);
        list.Should().Contain(42);
    }
}