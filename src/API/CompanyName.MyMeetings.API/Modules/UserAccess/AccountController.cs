using System.Security.Claims;
using CompanyName.MyMeetings.API.Configuration.Authorization;
using CompanyName.MyMeetings.Modules.UserAccess.Application.Authentication.Authenticate;
using CompanyName.MyMeetings.Modules.UserAccess.Application.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace CompanyName.MyMeetings.API.Modules.UserAccess;

[Route("account")]
public class AccountController : Controller
{
    private readonly IUserAccessModule _userAccessModule;
    private readonly IConfiguration _configuration;

    public AccountController(IUserAccessModule userAccessModule, IConfiguration configuration)
    {
        _userAccessModule = userAccessModule;
        _configuration = configuration;
    }

    [NoPermissionRequired]
    [HttpGet("login")]
    public IActionResult Login(string returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [NoPermissionRequired]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var authenticationResult = await _userAccessModule.ExecuteCommandAsync(
            new AuthenticateCommand(model.Username, model.Password));

        if (!authenticationResult.IsAuthenticated)
        {
            ModelState.AddModelError(string.Empty, authenticationResult.AuthenticationError);
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(OpenIddictConstants.Claims.Subject, authenticationResult.User.Id.ToString()),
            new(OpenIddictConstants.Claims.Name, authenticationResult.User.Name),
            new(OpenIddictConstants.Claims.Email, authenticationResult.User.Email)
        };

        claims.AddRange(authenticationResult.User.Claims);

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name,
            CustomClaimTypes.Roles);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        //TODO: Czy tu powinien być redirect do returnUrl, czy tylko do GetClientUrl()? W teorii API wie że przekierowanie ma być zawsze do aplikacji klienckiej. A co z mobile?
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return Redirect(GetClientUrl());
    }

    [NoPermissionRequired]
    [HttpGet("logout")]
    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout(string returnUrl)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (IsAllowedClientUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect(GetClientUrl());
    }

    private string GetClientUrl()
    {
        //TODO: Wartość domyślna powinna być w appsettings.json, a nie w kodzie. Wartość domyślna powinna być też w konfiguracji dla środowiska produkcyjnego.
        return _configuration["Auth:ClientUrl"] ?? "http://localhost:4200";
    }

    private bool IsAllowedClientUrl(string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || !Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var allowed = _configuration.GetSection("Auth:PostLogoutRedirectUris").Get<string[]>() ?? [];
        return allowed.Any(allowedUri =>
            Uri.TryCreate(allowedUri, UriKind.Absolute, out var allowedParsed) &&
            string.Equals(uri.GetLeftPart(UriPartial.Path).TrimEnd('/'), allowedParsed.GetLeftPart(UriPartial.Path).TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }
}