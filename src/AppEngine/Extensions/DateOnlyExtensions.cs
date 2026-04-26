namespace AppEngine.Extensions;

public static class DateOnlyExtensions
{
    extension(DateOnly dateOnly)
    {
        public DateTimeOffset ToDateTimeOffset(TimeZoneInfo? zone = null)
        {
            var dateTime = dateOnly.ToDateTime(TimeOnly.MinValue);
            return new DateTimeOffset(dateTime, zone?.GetUtcOffset(dateTime) ?? TimeSpan.Zero);
        }
    }
}