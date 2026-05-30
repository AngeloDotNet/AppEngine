using AppEngine.EFCore.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AppEngine.EFCore.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserName => httpContextAccessor.HttpContext?.User?.Identity?.Name;
}