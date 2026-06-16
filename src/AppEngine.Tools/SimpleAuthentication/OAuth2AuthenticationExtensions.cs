using AppEngine.Tools.OpenApi.SimpleAuthentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Tools.SimpleAuthentication;

public static class OAuth2AuthenticationExtensions
{
    extension(OpenApiOptions options)
    {
        public void AddOAuth2Authentication(string name, string authorizationUrl, string tokenUrl, IDictionary<string, string> scopes)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(authorizationUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(tokenUrl);
            ArgumentNullException.ThrowIfNull(scopes);

            options.AddOAuth2Authentication(name, new()
            {
                AuthorizationUrl = new(authorizationUrl),
                TokenUrl = new(tokenUrl),
                Scopes = scopes
            });
        }

        internal void AddOAuth2Authentication(string name, OpenApiOAuthFlow authFlow)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(authFlow);

            options.AddDocumentTransformer(new OAuth2AuthenticationDocumentTransformer(name, authFlow));
        }
    }
}