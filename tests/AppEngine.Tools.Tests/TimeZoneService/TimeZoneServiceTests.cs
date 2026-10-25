using Microsoft.AspNetCore.Http;
using AppEngine.Tools.TimeZoneService;

namespace AppEngine.Tools.Tests.TimeZoneService;

public class TimeZoneServiceTests
{
    private static AppEngine.Tools.TimeZoneService.TimeZoneService Create(string? headerValue, bool withHttpContext = true)
    {
        var accessor = new HttpContextAccessor();

        if (withHttpContext)
        {
            var context = new DefaultHttpContext();
            if (headerValue is not null)
            {
                context.Request.Headers[AppEngine.Tools.TimeZoneService.TimeZoneService.HeaderKey] = headerValue;
            }

            accessor.HttpContext = context;
        }

        return new AppEngine.Tools.TimeZoneService.TimeZoneService(accessor);
    }

    [Fact]
    public void HeaderKey_IsExpectedValue() =>
        Assert.Equal("x-time-zone", AppEngine.Tools.TimeZoneService.TimeZoneService.HeaderKey);

    [Theory]
    [InlineData("UTC")]
    [InlineData("Europe/Rome")]
    public void GetTimeZone_ValidId_ReturnsTimeZone(string id)
    {
        var timeZone = Create(id).GetTimeZone();

        Assert.NotNull(timeZone);
    }

    [Fact]
    public void GetTimeZone_HeaderMissing_ReturnsNull() =>
        Assert.Null(Create(null).GetTimeZone());

    [Fact]
    public void GetTimeZone_NoHttpContext_ReturnsNull() =>
        Assert.Null(Create(null, withHttpContext: false).GetTimeZone());

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("")]
    [InlineData("../../etc/passwd")]
    [InlineData("UTC\r\nInjected: header")]
    [InlineData("' OR 1=1 --")]
    public void GetTimeZone_InvalidOrMaliciousValue_ReturnsNull(string value) =>
        Assert.Null(Create(value).GetTimeZone());

    [Fact]
    public void GetTimeZone_VeryLongValue_ReturnsNull() =>
        Assert.Null(Create(new string('a', 100_000)).GetTimeZone());

    [Fact]
    public void GetTimeZoneHeaderValue_ReturnsRawHeader() =>
        Assert.Equal("Europe/Rome", Create("Europe/Rome").GetTimeZoneHeaderValue());

    [Fact]
    public void GetTimeZoneHeaderValue_HeaderMissing_ReturnsNull() =>
        Assert.Null(Create(null).GetTimeZoneHeaderValue());
}
