using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppEngine.Serialization;

public class ShortDateConverter(string? serializationFormat) : JsonConverter<DateTime>
{
    private readonly string serializationFormat = serializationFormat ?? "yyyy-MM-dd";

    public ShortDateConverter() : this(null)
    { }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateTimeOffset.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal).Date;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}