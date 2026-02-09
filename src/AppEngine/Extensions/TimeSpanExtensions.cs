namespace AppEngine.Extensions;

public static class TimeSpanExtensions
{
    public static TimeOnly ToTimeOnly(this TimeSpan timeSpan) => TimeOnly.FromTimeSpan(timeSpan);
}