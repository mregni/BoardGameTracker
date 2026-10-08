using System;
using System.Text.Json;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Datastore;

public class UtcDateTimeConvertersTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeConverter(), new UtcNullableDateTimeConverter() },
    };

    private sealed record Payload(DateTime When, DateTime? Maybe);

    [Fact]
    public void Json_ShouldWriteEveryDateTimeAsUtc()
    {
        var local = new DateTime(2026, 3, 2, 19, 0, 0, DateTimeKind.Local);

        var json = JsonSerializer.Serialize(new Payload(local, null), Options);
        var written = JsonSerializer.Deserialize<Payload>(json, Options)!;

        json.Should().EndWith("Z\",\"Maybe\":null}");
        written.When.Should().Be(local.ToUniversalTime());
        written.When.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("2026-03-02T19:00:00Z", 19)]
    [InlineData("2026-03-02T19:00:00+02:00", 17)]
    [InlineData("2026-03-02T19:00:00", 19)]
    public void Json_ShouldReadOffsetsIntoUtc_AndTreatUnspecifiedAsUtc(string value, int expectedUtcHour)
    {
        var parsed = JsonSerializer.Deserialize<Payload>($"{{\"When\":\"{value}\",\"Maybe\":\"{value}\"}}", Options)!;

        parsed.When.Kind.Should().Be(DateTimeKind.Utc);
        parsed.When.Hour.Should().Be(expectedUtcHour);
        parsed.Maybe.Should().Be(parsed.When);
    }

    [Fact]
    public void Json_ShouldRoundTripNull_ForNullableDates()
    {
        var parsed = JsonSerializer.Deserialize<Payload>("{\"When\":\"2026-03-02T19:00:00Z\",\"Maybe\":null}", Options)!;

        parsed.Maybe.Should().BeNull();
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void EfConverter_ShouldStoreUtc_AndReadBackAsUtc(DateTimeKind kind)
    {
        var converter = new UtcDateTimeValueConverter();
        var value = new DateTime(2026, 3, 2, 19, 0, 0, kind);

        var stored = (DateTime)converter.ConvertToProvider(value)!;
        var read = (DateTime)converter.ConvertFromProvider(DateTime.SpecifyKind(stored, DateTimeKind.Unspecified))!;

        stored.Kind.Should().Be(DateTimeKind.Utc);
        stored.Should().Be(kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc));
        read.Kind.Should().Be(DateTimeKind.Utc);
        read.Should().Be(stored);
    }

    [Fact]
    public void NullableEfConverter_ShouldPassNullThrough_AndNormaliseValues()
    {
        var converter = new UtcNullableDateTimeValueConverter();
        var local = new DateTime(2026, 3, 2, 19, 0, 0, DateTimeKind.Local);

        var storedNull = converter.ConvertToProvider(null);
        var stored = (DateTime?)converter.ConvertToProvider(local);
        var read = (DateTime?)converter.ConvertFromProvider(DateTime.SpecifyKind(stored!.Value, DateTimeKind.Unspecified));

        storedNull.Should().BeNull();
        stored.Should().Be(local.ToUniversalTime());
        read!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }
}
