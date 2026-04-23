using System.ComponentModel;
using System.Security.Claims;

namespace AppEngine.Tools.SimpleAuthentication.Abstractions;

public static class ClaimExtensions
{
    extension(IList<Claim> claims)
    {
        public void Update(string type, string value)
        {
            claims.Remove(type);
            claims.Add(new Claim(type, value));
        }

        public bool Remove(string type)
        {
            var claim = claims.FirstOrDefault(c => c.Type == type);
            return claims.Remove(claim!);
        }
    }

    extension(ClaimsPrincipal user)
    {
        public IEnumerable<string?> GetClaimValues(string type) => user.GetClaimValues<string>(type);

        public IEnumerable<T?> GetClaimValues<T>(string type)
        {
            var value = user.FindAll(type).Select(c => Convert<T>(c.Value)).ToList();
            return value;
        }

        public string? GetClaimValue(string type) => user.GetClaimValue<string>(type);

        public T? GetClaimValue<T>(string type)
        {
            var value = user.FindFirstValue(type);

            if (value is null)
            {
                return default;
            }

            return Convert<T>(value);
        }

        public bool HasClaim(string type)
        {
            var hasClaim = user.Claims.Any(c => c.Type == type);
            return hasClaim;
        }
    }

    private static T? Convert<T>(string value) => (T?)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value);
}