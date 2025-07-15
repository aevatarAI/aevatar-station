using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Aevatar.AuthServer.Grants;

public abstract class GrantHandlerBase : ITokenExtensionGrant
{
    public abstract string Name { get; }
    
    public abstract Task<IActionResult> HandleAsync(ExtensionGrantContext context);

    protected ForbidResult CreateForbidResult(string errorDescription)
    {
        return CreateForbidResult(OpenIddictConstants.Errors.InvalidRequest, errorDescription);
    }

    protected ForbidResult CreateForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }));
    }

    protected async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context,
        ImmutableArray<string> scopes)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>()
                           .ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
    }

    protected async Task<ClaimsPrincipal> CreateUserClaimsPrincipalWithFactoryAsync(ExtensionGrantContext context, IdentityUser user, bool isNewUser = false)
    {
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
        var claimsPrincipal = await signInManager.CreateUserPrincipalAsync(user);

        claimsPrincipal.AddClaim("is_new_user", isNewUser);
        
        claimsPrincipal.SetScopes(context.Request.GetScopes());
        claimsPrincipal.SetResources(await GetResourcesAsync(context, claimsPrincipal.GetScopes()));
        claimsPrincipal.SetAudiences("Aevatar");

        await context.HttpContext.RequestServices
            .GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, claimsPrincipal);

        return claimsPrincipal;
    }

    protected async Task<ClaimsPrincipal> CreateUserClaimsPrincipalWithSignInManagerAsync(ExtensionGrantContext context, IdentityUser user)
    {
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
        var claimsPrincipal = await signInManager.CreateUserPrincipalAsync(user);
        
        claimsPrincipal.SetScopes(context.Request.GetScopes());
        claimsPrincipal.SetResources(await GetResourcesAsync(context, claimsPrincipal.GetScopes()));
        claimsPrincipal.SetAudiences("Aevatar");
        
        await context.HttpContext.RequestServices
            .GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, claimsPrincipal);

        return claimsPrincipal;
    }
} 