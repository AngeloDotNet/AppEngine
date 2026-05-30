namespace AppEngine.EFCore.Services.Interfaces;

public interface ICurrentUserService
{
    string? UserName { get; }
}