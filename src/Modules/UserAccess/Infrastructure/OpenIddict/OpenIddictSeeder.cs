using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;

namespace CompanyName.MyMeetings.Modules.UserAccess.Infrastructure.OpenIddict;

internal sealed class OpenIddictSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public OpenIddictSeeder(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OpenIddictDbContext>();
        await ApplyMigrationsAsync(context, cancellationToken);

        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        if (await scopeManager.FindByNameAsync(OpenIddictConfig.ApiScope, cancellationToken) is null)
        {
            await scopeManager.CreateAsync(
                new OpenIddictScopeDescriptor
                {
                    Name = OpenIddictConfig.ApiScope,
                    DisplayName = "MyMeetings API",
                    Resources = { OpenIddictConfig.ApiResource }
                },
                cancellationToken);
        }

        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var descriptor = CreateApplicationDescriptor();
        var existingApplication = await applicationManager.FindByClientIdAsync(OpenIddictConfig.SpaClientId, cancellationToken);
        if (existingApplication is null)
        {
            await applicationManager.CreateAsync(descriptor, cancellationToken);
        }
        else
        {
            await applicationManager.UpdateAsync(existingApplication, descriptor, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task ApplyMigrationsAsync(OpenIddictDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 2714)
        {
        }
    }

    private OpenIddictApplicationDescriptor CreateApplicationDescriptor()
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = OpenIddictConfig.SpaClientId,
            DisplayName = "MyMeetings SPA",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConfig.ApiScope
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var redirectUri in GetRedirectUris())
        {
            descriptor.RedirectUris.Add(redirectUri);
        }

        foreach (var postLogoutUri in GetPostLogoutRedirectUris())
        {
            descriptor.PostLogoutRedirectUris.Add(postLogoutUri);
        }

        return descriptor;
    }

    private IEnumerable<Uri> GetRedirectUris()
    {
        var configured = _configuration.GetSection("Auth:RedirectUris").Get<string[]>();
        if (configured is { Length: > 0 })
        {
            return configured.Select(uri => new Uri(uri));
        }

        return
        [
            new Uri("http://localhost:4200"),
            new Uri("http://localhost:5000/swagger/oauth2-redirect.html")
        ];
    }

    private IEnumerable<Uri> GetPostLogoutRedirectUris()
    {
        var configured = _configuration.GetSection("Auth:PostLogoutRedirectUris").Get<string[]>();
        if (configured is { Length: > 0 })
        {
            return configured.Select(uri => new Uri(uri));
        }

        return
        [
            new Uri("http://localhost:4200")
        ];
    }
}