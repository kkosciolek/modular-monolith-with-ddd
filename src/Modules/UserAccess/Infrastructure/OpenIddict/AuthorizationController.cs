using System.Security.Claims;
using CompanyName.MyMeetings.Modules.UserAccess.Application.Contracts;
using CompanyName.MyMeetings.Modules.UserAccess.Application.Users.GetUser;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace CompanyName.MyMeetings.Modules.UserAccess.Infrastructure.OpenIddict;

public class AuthorizationController : Controller
{
    private readonly IUserAccessModule _userAccessModule;

    public AuthorizationController(IUserAccessModule userAccessModule)
    {
        _userAccessModule = userAccessModule;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
            {
                return ForbidGrant(OpenIddictConstants.Errors.LoginRequired, "The user is not logged in.");
            }

            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: CustomClaimTypes.Roles);

        foreach (var claim in result.Principal.Claims)
        {
            identity.AddClaim(claim);
        }

        identity.SetScopes(request.GetScopes());
        identity.SetResources(OpenIddictConfig.ApiResource);
        identity.SetDestinations(GetDestinations);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal is null)
            {
                return ForbidGrant(OpenIddictConstants.Errors.InvalidGrant, "The token is no longer valid.");
            }

            var subject = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            if (string.IsNullOrEmpty(subject) || !Guid.TryParse(subject, out var userId))
            {
                return ForbidGrant(OpenIddictConstants.Errors.InvalidGrant, "The token is no longer valid.");
            }

            var user = await _userAccessModule.ExecuteQueryAsync(new GetUserQuery(userId));

            if (user is null || !user.IsActive)
            {
                return ForbidGrant(OpenIddictConstants.Errors.InvalidGrant, "The token is no longer valid.");
            }

            var identity = new ClaimsIdentity(
                result.Principal.Claims,
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: CustomClaimTypes.Roles);

            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return ForbidGrant(OpenIddictConstants.Errors.UnsupportedGrantType, "The specified grant type is not supported.");
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties());
    }

    private ForbidResult ForbidGrant(string error, string description)
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            "AspNet.Identity.SecurityStamp" => [],
            _ => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken]
        };
    }
}
