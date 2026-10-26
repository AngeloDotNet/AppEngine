namespace AppEngine.Extensions;

public static class DateTimeOffsetExtensions
{
    extension(DateTimeOffset dateTimeOffset)
    {
        public DateOnly ToDateOnly(TimeZoneInfo? zone = null)
        {
            var inTargetZone = TimeZoneInfo.ConvertTime(dateTimeOffset, zone ?? TimeZoneInfo.Utc);
            return DateOnly.FromDateTime(inTargetZone.Date);
        }

        public TimeOnly ToTimeOnly(TimeZoneInfo? zone = null)
        {
            return TimeOnly.FromDateTime(dateTimeOffset.DateTime);
        }
    }
}