using System.Security.Cryptography.X509Certificates;
using CompanyName.MyMeetings.Modules.UserAccess.Infrastructure.OpenIddict;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;

namespace CompanyName.MyMeetings.Modules.UserAccess.Infrastructure.Configuration.Identity;

public static class IdentityConfiguration
{
    public const string AuthenticationScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;

    public static IServiceCollection ConfigureIdentityService(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllersWithViews()
            .AddApplicationPart(typeof(AuthorizationController).Assembly);

        var connectionString = configuration.GetConnectionString("MeetingsConnectionString")
                               ?? throw new InvalidOperationException("Connection string 'MeetingsConnectionString' is not configured.");

        services.AddDbContext<OpenIddictDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("OpenIddictMigrationsHistory", "auth"));
            options.UseOpenIddict();
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<OpenIddictDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetEndSessionEndpointUris("connect/logout");

                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .RequireProofKeyForCodeExchange();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConfig.ApiScope);

                ConfigureCertificates(options, configuration, environment);

                options.DisableAccessTokenEncryption();
                options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));

                var aspNetCore = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();

                if (environment.IsDevelopment())
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/account/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        services.AddHostedService<OpenIddictSeeder>();

        return services;
    }

    private static void ConfigureCertificates(
        OpenIddictServerBuilder options,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
            return;
        }

        var signingPath = configuration["Auth:SigningCertificatePath"];
        if (string.IsNullOrWhiteSpace(signingPath))
        {
            throw new InvalidOperationException(
                "Auth:SigningCertificatePath is required outside Development.");
        }

        var signingPassword = configuration["Auth:SigningCertificatePassword"];
        var signingCertificate = X509CertificateLoader.LoadPkcs12FromFile(signingPath, signingPassword);
        options.AddSigningCertificate(signingCertificate);

        var encryptionPath = configuration["Auth:EncryptionCertificatePath"];
        if (string.IsNullOrWhiteSpace(encryptionPath))
        {
            options.AddEncryptionCertificate(signingCertificate);
            return;
        }

        var encryptionPassword = configuration["Auth:EncryptionCertificatePassword"];
        options.AddEncryptionCertificate(
            X509CertificateLoader.LoadPkcs12FromFile(encryptionPath, encryptionPassword));
    }
}
