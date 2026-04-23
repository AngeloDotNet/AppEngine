using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace AppEngine.Tools.SimpleAuthentication.JwtBearer;

public class JwtBearerValidationResult
{
    [MemberNotNullWhen(true, nameof(Principal))]
    [MemberNotNullWhen(false, nameof(Exception))]
    public bool IsValid { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public Exception? Exception { get; set; }
}