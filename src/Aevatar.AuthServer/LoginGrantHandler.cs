using System.Collections.Immutable;
using Aevatar.OpenIddict;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace Aevatar;

public class LoginGrantHandler: ITokenExtensionGrant
{
    private ILogger<LoginGrantHandler>? _logger;

    public string Name { get; } = GrantTypeConstants.LOGIN;

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var scopeManager = context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>();
        var abpOpenIddictClaimDestinationsManager = context.HttpContext.RequestServices
            .GetRequiredService<AbpOpenIddictClaimsPrincipalManager>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<IdentityUser>>();
        var identityUserManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        var name = context.Request.GetParameter("name")?.ToString();
        var password = context.Request.GetParameter("password").ToString();
        if (name.IsNullOrEmpty() || password.IsNullOrEmpty())
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "invalid name or password"
                }));
        }

        var identityUser = await identityUserManager.FindByNameAsync(name);
        if (identityUser == null)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "invalid user"
                }));
        }

        var result = await identityUserManager.CheckPasswordAsync(identityUser, password);

        if (!result)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "invalid name or password"
                }));
        }

        var principal = await signInManager.CreateUserPrincipalAsync(identityUser);

        principal.SetScopes(context.Request.GetScopes());
        principal.SetResources(await GetResourcesAsync(context.Request.GetScopes(), scopeManager));
        principal.SetAudiences("Aevatar");

        await abpOpenIddictClaimDestinationsManager.HandleAsync(context.Request, principal);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }

    protected virtual async Task<IEnumerable<string>> GetResourcesAsync(ImmutableArray<string> scopes,
        IOpenIddictScopeManager scopeManager)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in scopeManager.ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
    }
}