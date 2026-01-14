using System.Text.Json;
using BoardGameTracker.Api.Infrastructure;
using Xunit;

namespace BoardGameTracker.Tests.Api;

public class DateTimeJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public DateTimeJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new UtcDateTimeConverter(), new UtcNullableDateTimeConverter() }
        };
    }

    [Fact]
    public void Deserialize_UtcDateTime_ShouldParseCorrectly()
    {
        // Arrange
        var json = """{"date":"2026-01-07T13:30:00Z"}""";

        // Act
        var result = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc), result.Date);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    [Fact]
    public void Deserialize_DateTimeWithTimezone_ShouldConvertToUtc()
    {
        // Arrange - Brussels time (UTC+1)
        var json = """{"date":"2026-01-07T14:30:00+01:00"}""";

        // Act
        var result = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc), result.Date);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    [Fact]
    public void Deserialize_DateTimeWithNegativeOffset_ShouldConvertToUtc()
    {
        // Arrange - New York time (UTC-5)
        var json = """{"date":"2026-01-07T08:30:00-05:00"}""";

        // Act
        var result = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc), result.Date);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    [Fact]
    public void Deserialize_DateTimeWithoutTimezone_ShouldAssumeUtc()
    {
        // Arrange - No timezone info
        var json = """{"date":"2026-01-07T13:30:00"}""";

        // Act
        var result = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    [Fact]
    public void Serialize_UtcDateTime_ShouldIncludeZSuffix()
    {
        // Arrange
        var dto = new TestDto
        {
            Date = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);

        // Assert
        Assert.Contains("2026-01-07T13:30:00Z", json);
    }

    [Fact]
    public void Serialize_LocalDateTime_ShouldConvertToUtc()
    {
        // Arrange
        var localTime = new DateTime(2026, 1, 7, 14, 30, 0, DateTimeKind.Local);
        var dto = new TestDto { Date = localTime };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);
        var result = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
        // Should have been converted to UTC
        Assert.NotEqual(localTime, result.Date);
    }

    [Fact]
    public void RoundTrip_UtcDateTime_ShouldPreserveValue()
    {
        // Arrange
        var original = new TestDto
        {
            Date = new DateTime(2026, 1, 7, 13, 30, 45, 123, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var result = JsonSerializer.Deserialize<TestDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        // Note: Milliseconds might be lost in JSON serialization depending on format
        Assert.Equal(original.Date.Year, result.Date.Year);
        Assert.Equal(original.Date.Month, result.Date.Month);
        Assert.Equal(original.Date.Day, result.Date.Day);
        Assert.Equal(original.Date.Hour, result.Date.Hour);
        Assert.Equal(original.Date.Minute, result.Date.Minute);
        Assert.Equal(original.Date.Second, result.Date.Second);
        Assert.Equal(DateTimeKind.Utc, result.Date.Kind);
    }

    [Fact]
    public void Deserialize_NullableDateTime_WithNull_ShouldReturnNull()
    {
        // Arrange
        var json = """{"nullableDate":null}""";

        // Act
        var result = JsonSerializer.Deserialize<TestNullableDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.NullableDate);
    }

    [Fact]
    public void Deserialize_NullableDateTime_WithValue_ShouldReturnUtc()
    {
        // Arrange
        var json = """{"nullableDate":"2026-01-07T13:30:00Z"}""";

        // Act
        var result = JsonSerializer.Deserialize<TestNullableDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.NullableDate);
        Assert.Equal(DateTimeKind.Utc, result.NullableDate.Value.Kind);
    }

    [Fact]
    public void Serialize_NullableDateTime_WithNull_ShouldSerializeNull()
    {
        // Arrange
        var dto = new TestNullableDto { NullableDate = null };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);

        // Assert
        Assert.Contains("\"nullableDate\":null", json);
    }

    [Fact]
    public void Serialize_NullableDateTime_WithValue_ShouldSerializeUtc()
    {
        // Arrange
        var dto = new TestNullableDto
        {
            NullableDate = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);

        // Assert
        Assert.Contains("2026-01-07T13:30:00Z", json);
    }

    private class TestDto
    {
        public DateTime Date { get; set; }
    }

    private class TestNullableDto
    {
        public DateTime? NullableDate { get; set; }
    }
}
