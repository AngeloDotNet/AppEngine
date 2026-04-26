namespace AppEngine.Extensions;

public static class TimeSpanExtensions
{
    extension(TimeSpan timeSpan)
    {
        public TimeOnly ToTimeOnly() => TimeOnly.FromTimeSpan(timeSpan);
    }
}