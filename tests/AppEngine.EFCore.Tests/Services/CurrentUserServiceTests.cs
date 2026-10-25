using System.Security.Claims;
using AppEngine.EFCore.Services;
using Microsoft.AspNetCore.Http;

namespace AppEngine.EFCore.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserName_NoHttpContext_ReturnsNull() =>
        Assert.Null(new CurrentUserService(new HttpContextAccessor()).UserName);

    [Fact]
    public void UserName_Anonymous_ReturnsNull()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        Assert.Null(new CurrentUserService(accessor).UserName);
    }

    [Fact]
    public void UserName_Authenticated_ReturnsIdentityName()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "mario")], "test"))
        };

        Assert.Equal("mario", new CurrentUserService(new HttpContextAccessor { HttpContext = context }).UserName);
    }
}
