using AppEngine.Tools.TimeZoneService.Interfaces;

namespace AppEngine.Tools.TimeZoneService;

public class ClientTimeProvider(ITimeZoneService timeZoneService) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => timeZoneService.GetTimeZone() ?? TimeZoneInfo.Utc;
}