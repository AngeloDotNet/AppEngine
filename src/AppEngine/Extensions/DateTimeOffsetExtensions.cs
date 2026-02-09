namespace AppEngine.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateOnly ToDateOnly(this DateTimeOffset dateTimeOffset, TimeZoneInfo? zone = null)
    {
        var inTargetZone = TimeZoneInfo.ConvertTime(dateTimeOffset, zone ?? TimeZoneInfo.Utc);
        return DateOnly.FromDateTime(inTargetZone.Date);
    }

    public static TimeOnly ToTimeOnly(this DateTimeOffset dateTimeOffset, TimeZoneInfo? zone = null)
    {
        var inTargetZone = TimeZoneInfo.ConvertTime(dateTimeOffset, zone ?? TimeZoneInfo.Utc);
        return TimeOnly.FromDateTime(dateTimeOffset.DateTime);
    }
}