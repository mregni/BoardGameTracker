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
        result.Available.Should().BeTrue();
    }

    [Theory]
    [InlineData("in stock: true - price: 5", true)]
    [InlineData("IN STOCK: FALSE - PRICE: 5", false)]
    public void Parse_ShouldIgnoreCase(string content, bool expected)
    {
        var result = ChangeDetectionSnapshotParser.Parse(content);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().Be(expected);
        result.Price.Should().Be(5m);
    }

    [Fact]
    public void Parse_ShouldTreatLowercaseNoneAsUnknownStock()
    {
        var result = ChangeDetectionSnapshotParser.Parse("In Stock: none - Price: 5");

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().BeNull();
        result.Price.Should().Be(5m);
    }

    [Fact]
    public void Parse_ShouldFindTheLineInsideASurroundingSnapshot_AndIgnoreTextAfterThePrice()
    {
        var result = ChangeDetectionSnapshotParser.Parse("Header\nIn Stock:True-Price:12.50 EUR\nFooter");

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().BeTrue();
        result.Price.Should().Be(12.5m);
    }

    [Theory]
    [InlineData("In Stock: True - Price: 12,50", 12.5)]
    [InlineData("In Stock: True - Price: 1234,56", 1234.56)]
    [InlineData("In Stock: True - Price: 1234.567", 1234.567)]
    [InlineData("In Stock: True - Price: 1299", 1299)]
    [InlineData("In Stock: True - Price: 1.299", 1299)]
    public void Parse_ShouldParseUnambiguousSeparators(string content, double expected)
    {
        var result = ChangeDetectionSnapshotParser.Parse(content);

        result.Price.Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("In Stock: True - Price: 1,299.99")]
    [InlineData("In Stock: True - Price: 1.299,99")]
    [InlineData("In Stock: True - Price: 1,234")]
    public void Parse_ShouldRejectAmbiguousPrice_RatherThanMangleIt(string content)
    {
        var result = ChangeDetectionSnapshotParser.Parse(content);

        result.Available.Should().BeTrue();
        result.InStock.Should().BeTrue();
        result.Price.Should().BeNull();
    }

    [Theory]
    [InlineData("39.99", 39.99)]
    [InlineData(" 12,50 ", 12.5)]
    [InlineData("1299", 1299)]
    [InlineData("0", 0)]
    [InlineData("1,2", 1.2)]
    [InlineData("1,23", 1.23)]
    [InlineData("1,2345", 1.2345)]
    public void ParsePrice_ShouldParseAPlainAmount(string raw, double expected)
    {
        ChangeDetectionSnapshotParser.ParsePrice(raw).Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-5")]
    [InlineData("+5")]
    [InlineData("1,234")]
    [InlineData("1.299,99")]
    [InlineData("12.50 EUR")]
    [InlineData("EUR 12.50")]
    [InlineData("1e3")]
    [InlineData("   ")]
    [InlineData("1,2,3")]
    [InlineData("1.2.3")]
    [InlineData("99999999999999999999999999999999")]
    public void ParsePrice_ShouldRejectSignedAmbiguousOrDecoratedValues(string? raw)
    {
        ChangeDetectionSnapshotParser.ParsePrice(raw).Should().BeNull();
    }

    [Theory]
    [InlineData("1.299", 1299)]
    [InlineData("12.500", 12500)]
    [InlineData("999.999", 999999)]
    public void ParsePrice_ShouldReadADotBeforeThreeDigitsAsAThousandsSeparator(string raw, int expected)
    {
        ChangeDetectionSnapshotParser.ParsePrice(raw).Should().Be(expected);
    }

    [Theory]
    [InlineData("0.299", 0.299)]
    [InlineData("1234.567", 1234.567)]
    [InlineData("1.29", 1.29)]
    [InlineData("1.2999", 1.2999)]
    public void ParsePrice_ShouldKeepADecimalPoint_WhenItCannotBeAThousandsSeparator(string raw, double expected)
    {
        ChangeDetectionSnapshotParser.ParsePrice(raw).Should().Be((decimal)expected);
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
