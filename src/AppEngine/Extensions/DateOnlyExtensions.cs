namespace AppEngine.Extensions;

public static class DateOnlyExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this DateOnly dateOnly, TimeZoneInfo? zone = null)
    {
        var dateTime = dateOnly.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(dateTime, zone?.GetUtcOffset(dateTime) ?? TimeSpan.Zero);
    }
}