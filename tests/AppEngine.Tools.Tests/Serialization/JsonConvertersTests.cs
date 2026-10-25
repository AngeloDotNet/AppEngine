using System.Text.Json;
using AppEngine.Tools.Serialization;

namespace AppEngine.Tools.Tests.Serialization;

public class JsonConvertersTests
{
    private static JsonSerializerOptions Options(params System.Text.Json.Serialization.JsonConverter[] converters)
    {
        var options = new JsonSerializerOptions();
        foreach (var converter in converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    // ShortDateConverter

    [Fact]
    public void ShortDate_Write_UsesDateOnlyFormat()
    {
        var json = JsonSerializer.Serialize(new DateTime(2025, 3, 4, 15, 30, 0), Options(new ShortDateConverter()));

        Assert.Equal("\"2025-03-04\"", json);
    }

    [Fact]
    public void ShortDate_Write_CustomFormat()
    {
        var json = JsonSerializer.Serialize(new DateTime(2025, 3, 4), Options(new ShortDateConverter("yyyy/MM/dd")));

        Assert.Equal("\"2025/03/04\"", json);
    }

    [Theory]
    [InlineData("\"2025-03-04\"")]
    [InlineData("\"2025-03-04T10:15:00Z\"")]
    public void ShortDate_Read_ReturnsDatePart(string json)
    {
        var value = JsonSerializer.Deserialize<DateTime>(json, Options(new ShortDateConverter()));

        Assert.Equal(new DateTime(2025, 3, 4), value);
    }

    [Theory]
    [InlineData("\"not-a-date\"")]
    [InlineData("null")]
    [InlineData("\"' OR 1=1 --\"")]
    public void ShortDate_Read_InvalidInput_Throws(string json) =>
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<DateTime>(json, Options(new ShortDateConverter())));

    // StringTrimmingConverter

    [Fact]
    public void StringTrimming_Read_TrimsValue()
    {
        var value = JsonSerializer.Deserialize<string>("\"  hello world \\t\"", Options(new StringTrimmingConverter()));

        Assert.Equal("hello world", value);
    }

    [Fact]
    public void StringTrimming_Read_Null_ReturnsNull() =>
        Assert.Null(JsonSerializer.Deserialize<string>("null", Options(new StringTrimmingConverter())));

    [Fact]
    public void StringTrimming_Write_TrimsValue()
    {
        var json = JsonSerializer.Serialize("  hello ", Options(new StringTrimmingConverter()));

        Assert.Equal("\"hello\"", json);
    }

    [Fact]
    public void StringTrimming_Read_DoesNotAlterInnerContent()
    {
        var value = JsonSerializer.Deserialize<string>("\" <b>a  b</b> \"", Options(new StringTrimmingConverter()));

        Assert.Equal("<b>a  b</b>", value);
    }

    // TimeSpanTicksConverter

    [Fact]
    public void TimeSpanTicks_RoundTrips()
    {
        var options = Options(new TimeSpanTicksConverter());
        var value = TimeSpan.FromMinutes(90);

        var json = JsonSerializer.Serialize(value, options);

        Assert.Equal(value.Ticks.ToString(), json);
        Assert.Equal(value, JsonSerializer.Deserialize<TimeSpan>(json, options));
    }

    [Fact]
    public void TimeSpanTicks_Read_NonNumber_Throws() =>
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<TimeSpan>("\"01:00:00\"", Options(new TimeSpanTicksConverter())));

    // UtcDateTimeConverter

    [Fact]
    public void UtcDateTime_Write_Utc_UsesIsoFormatWithZ()
    {
        var json = JsonSerializer.Serialize(new DateTime(2025, 3, 4, 10, 0, 0, DateTimeKind.Utc), Options(new UtcDateTimeConverter()));

        Assert.Equal("\"2025-03-04T10:00:00.0000000Z\"", json);
    }

    [Fact]
    public void UtcDateTime_Write_Local_IsConvertedToUtc()
    {
        var local = new DateTime(2025, 3, 4, 10, 0, 0, DateTimeKind.Local);

        var json = JsonSerializer.Serialize(local, Options(new UtcDateTimeConverter()));

        var expected = local.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'");
        Assert.Equal($"\"{expected}\"", json);
    }

    [Fact]
    public void Current_UtcDateTime_Write_Unspecified_IsLabelledUtcWithoutConversion()
    {
        // Documents current behavior: an Unspecified DateTime is written as-is with a "Z" suffix.
        var json = JsonSerializer.Serialize(new DateTime(2025, 3, 4, 10, 0, 0, DateTimeKind.Unspecified), Options(new UtcDateTimeConverter()));

        Assert.Equal("\"2025-03-04T10:00:00.0000000Z\"", json);
    }

    [Fact]
    public void UtcDateTime_Read_Offset_IsConvertedToUtc()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2025-03-04T10:00:00+01:00\"", Options(new UtcDateTimeConverter()));

        Assert.Equal(new DateTime(2025, 3, 4, 9, 0, 0, DateTimeKind.Utc), value);
        Assert.Equal(DateTimeKind.Utc, value.Kind);
    }

    [Fact]
    public void UtcDateTime_Write_CustomFormat()
    {
        var json = JsonSerializer.Serialize(new DateTime(2025, 3, 4, 10, 0, 0, DateTimeKind.Utc), Options(new UtcDateTimeConverter("yyyy-MM-dd HH:mm")));

        Assert.Equal("\"2025-03-04 10:00\"", json);
    }
}
