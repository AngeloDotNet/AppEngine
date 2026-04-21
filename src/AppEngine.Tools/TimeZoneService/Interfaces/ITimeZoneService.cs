namespace AppEngine.Tools.TimeZoneService.Interfaces;

public interface ITimeZoneService
{
    string? GetTimeZoneHeaderValue();
    TimeZoneInfo? GetTimeZone();
}