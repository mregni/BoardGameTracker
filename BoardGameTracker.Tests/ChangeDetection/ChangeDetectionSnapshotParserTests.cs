using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ChangeDetection;

public class ChangeDetectionSnapshotParserTests
{
    [Fact]
    public void Parse_ShouldReturnInStockAndPrice_WhenInStockWithPrice()
    {
        var result = ChangeDetectionSnapshotParser.Parse("In Stock: True - Price: 22.5");

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.Available.Should().BeTrue();
        result.InStock.Should().BeTrue();
        result.Price.Should().Be(22.5m);
    }

    [Fact]
    public void Parse_ShouldReturnOutOfStock_WhenNotInStock()
    {
        var result = ChangeDetectionSnapshotParser.Parse("In Stock: False - Price: 19.99");

        result.Available.Should().BeTrue();
        result.InStock.Should().BeFalse();
        result.Price.Should().Be(19.99m);
    }

    [Fact]
    public void Parse_ShouldReturnUnknownStockWithPrice_WhenStockIsNone()
    {
        var result = ChangeDetectionSnapshotParser.Parse("In Stock: None - Price: 22.5");

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.Available.Should().BeTrue();
        result.InStock.Should().BeNull();
        result.Price.Should().Be(22.5m);
    }

    [Fact]
    public void Parse_ShouldReturnNullPrice_WhenPriceEmpty()
    {
        var result = ChangeDetectionSnapshotParser.Parse("In Stock: True - Price: ");

        result.Available.Should().BeTrue();
        result.InStock.Should().BeTrue();
        result.Price.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldHandleCommaDecimalSeparator()
    {
        var result = ChangeDetectionSnapshotParser.Parse("In Stock: True - Price: 22,5");

        result.Price.Should().Be(22.5m);
    }

    [Theory]
    [InlineData("In Stock: True - Price: 1,299.99")]
    [InlineData("In Stock: True - Price: 1.299,99")]
    public void Parse_ShouldRejectAmbiguousPrice_RatherThanMangleIt(string content)
    {
        var result = ChangeDetectionSnapshotParser.Parse(content);

        result.Available.Should().BeTrue();
        result.InStock.Should().BeTrue();
        result.Price.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("some unrelated snapshot text")]
    [InlineData("<html><body>Not a restock watch</body></html>")]
    public void Parse_ShouldReturnParseError_WhenMalformedOrEmpty(string? content)
    {
        var result = ChangeDetectionSnapshotParser.Parse(content);

        result.Status.Should().Be(ChangeDetectionStatus.ParseError);
        result.Available.Should().BeFalse();
        result.InStock.Should().BeNull();
        result.Price.Should().BeNull();
    }
}
