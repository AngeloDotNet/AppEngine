using System.Text;
using AppEngine.Tools.SimpleAuthentication.JwtBearer;
using AppEngine.Tools.SimpleAuthentication.JwtBearer.Interfaces;
using AppEngine.Tools.SimpleAuthentication.JwtBearer.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AppEngine.Tools.SimpleAuthentication;

public static class SimpleAuthenticationExtensions
{
    extension(IServiceCollection services)
    {
        public AuthenticationBuilder AddSimpleAuthentication(IConfiguration configuration, string sectionName, bool addAuthorizationServices = true)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

            var defaultAuthenticationScheme = configuration.GetValue<string?>($"{sectionName}:DefaultScheme");

            if (string.IsNullOrWhiteSpace(defaultAuthenticationScheme))
            {
                defaultAuthenticationScheme = null; // treat empty/whitespace as no value.
            }

            var builder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = defaultAuthenticationScheme;
                options.DefaultChallengeScheme = defaultAuthenticationScheme;
            });

            return builder.AddSimpleAuthentication(configuration, sectionName, addAuthorizationServices);
        }
    }

    extension(AuthenticationBuilder builder)
    {
        public AuthenticationBuilder AddSimpleAuthentication(IConfiguration configuration, string sectionName, bool addAuthorizationServices = true)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

            if (addAuthorizationServices)
            {
                builder.Services.AddAuthorization();
            }

            CheckAddJwtBearer(builder, configuration.GetSection($"{sectionName}:JwtBearer"));

            return builder;
        }
    }

    private static void CheckAddJwtBearer(AuthenticationBuilder builder, IConfigurationSection section)
    {
        var settings = section.Get<JwtBearerSettings>();

        if (settings is null)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(settings.SchemeName, nameof(JwtBearerSettings.SchemeName));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.SecurityKey, nameof(JwtBearerSettings.SecurityKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Algorithm, nameof(JwtBearerSettings.Algorithm));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.NameClaimType, nameof(JwtBearerSettings.NameClaimType));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.RoleClaimType, nameof(JwtBearerSettings.RoleClaimType));

        builder.Services.Configure<JwtBearerSettings>(section);

        builder.AddJwtBearer(settings.SchemeName, options =>
        {
            options.TokenValidationParameters = new()
            {
                AuthenticationType = settings.SchemeName,
                NameClaimType = settings.NameClaimType,
                RoleClaimType = settings.RoleClaimType,
                ValidateIssuer = settings.Issuers?.Length > 0,
                ValidIssuers = settings.Issuers,
                ValidateAudience = settings.Audiences?.Length > 0,
                ValidAudiences = settings.Audiences,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey)),
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = settings.ClockSkew
            };
        });

        builder.Services.TryAddSingleton<IJwtBearerService, JwtBearerService>();
    }
}