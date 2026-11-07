using System.Text;
using System.Text.Json;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

[Collection(EnvironmentVariablesCollection.Name)]
public class UtcDateTimeConverterTests
{
    private static readonly JsonSerializerOptions Options = new();
    private static readonly DateTime UtcValue = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    private static DateTime Read(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return new UtcDateTimeConverter().Read(ref reader, typeof(DateTime), Options);
    }

    private static DateTime? ReadNullable(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return new UtcNullableDateTimeConverter().Read(ref reader, typeof(DateTime?), Options);
    }

    private static string Written(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Write(DateTime value) =>
        Written(writer => new UtcDateTimeConverter().Write(writer, value, Options));

    private static string WriteNullable(DateTime? value) =>
        Written(writer => new UtcNullableDateTimeConverter().Write(writer, value, Options));

    [Theory]
    [InlineData("\"2026-03-02T19:00:00Z\"")]
    [InlineData("\"2026-03-02T21:00:00+02:00\"")]
    [InlineData("\"2026-03-02T14:00:00-05:00\"")]
    [InlineData("\"2026-03-02T19:00:00\"")]
    public void Read_ShouldAlwaysReturnTheUtcMoment(string json)
    {
        var result = Read(json);

        result.Should().Be(UtcValue);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("\"2026-03-02T19:00:00Z\"")]
    [InlineData("\"2026-03-02T21:00:00+02:00\"")]
    [InlineData("\"2026-03-02T14:00:00-05:00\"")]
    [InlineData("\"2026-03-02T19:00:00\"")]
    public void ReadNullable_ShouldAlwaysReturnTheUtcMoment(string json)
    {
        var result = ReadNullable(json);

        result.Should().Be(UtcValue);
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ReadNullable_ShouldReturnNull_ForAJsonNull()
    {
        ReadNullable("null").Should().BeNull();
    }

    [Theory]
    [InlineData("\"not-a-date\"")]
    [InlineData("\"2026-13-40\"")]
    public void Read_ShouldRefuseAStringThatIsNotADate(string json)
    {
        var act = () => Read(json);
        var actNullable = () => ReadNullable(json);

        act.Should().Throw<FormatException>();
        actNullable.Should().Throw<FormatException>();
    }

    [Fact]
    public void Write_ShouldKeepAUtcValue()
    {
        Write(UtcValue).Should().Be("\"2026-03-02T19:00:00Z\"");
        WriteNullable(UtcValue).Should().Be("\"2026-03-02T19:00:00Z\"");
    }

    [Fact]
    public void Write_ShouldConvertALocalValueToUtc()
    {
        var local = UtcValue.ToLocalTime();

        Write(local).Should().Be("\"2026-03-02T19:00:00Z\"");
        WriteNullable(local).Should().Be("\"2026-03-02T19:00:00Z\"");
    }

    [Fact]
    public void WriteNullable_ShouldWriteAJsonNull()
    {
        WriteNullable(null).Should().Be("null");
    }

    [Fact]
    public void Write_ShouldTreatAnUnspecifiedValueAsUtc_LikeRead()
    {
        var unspecified = DateTime.SpecifyKind(UtcValue, DateTimeKind.Unspecified);
        var original = Environment.GetEnvironmentVariable("TZ");
        Environment.SetEnvironmentVariable("TZ", "Asia/Tokyo");
        TimeZoneInfo.ClearCachedData();
        try
        {
            Write(unspecified).Should().Be("\"2026-03-02T19:00:00Z\"");
            WriteNullable(unspecified).Should().Be("\"2026-03-02T19:00:00Z\"");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", original);
            TimeZoneInfo.ClearCachedData();
        }
    }
}
