using System.Collections.Immutable;
using System.Security.Claims;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace Aevatar.AuthServer.Grants;

public class GithubGrantHandler : ITokenExtensionGrant
{
    private readonly ILogger<GithubGrantHandler> _logger;
    private readonly IGithubProvider _githubProvider;

    public string Name => GrantTypeConstants.Github;
    
    public GithubGrantHandler(IGithubProvider githubProvider, ILogger<GithubGrantHandler> logger)
    {
        _githubProvider = githubProvider;
        _logger = logger;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var code = context.Request.GetParameter("code").ToString();
        
        if (string.IsNullOrEmpty(code))
        {
            return ErrorResult("Missing code parameter");
        }

        var githubUser = await _githubProvider.GetUserInfoAsync(code);
        if (githubUser == null)
        {
            return ErrorResult("Invalid code");
        }
        
        var user = await GetOrCreateUserAsync(context, githubUser);
        if (user == null)
        {
            return ErrorResult("Failed to create or retrieve user");
        }
        
        var claimsPrincipal = await CreateUserClaimsPrincipalAsync(context, user);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }
    
    private async Task<IdentityUser> GetOrCreateUserAsync(ExtensionGrantContext context, GithubUser githubUser)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        
        var user = await userManager.FindByLoginAsync(GrantTypeConstants.Github, githubUser.Id.ToString());
        if (user != null)
        {
            return user;
        }

        if (!string.IsNullOrWhiteSpace(githubUser.Email))
        {
            user = await userManager.FindByEmailAsync(githubUser.Email);
        }

        if (user == null)
        {
            user = await CreateUserFromGithubAsync(githubUser, userManager);
        }

        await userManager.AddLoginAsync(user, new UserLoginInfo(
            GrantTypeConstants.Github, 
            githubUser.Id.ToString(), 
            GrantTypeConstants.Github));

        return user;
    }

    private async Task<IdentityUser> CreateUserFromGithubAsync(GithubUser githubUser, IdentityUserManager userManager)
    {
        var name = Guid.NewGuid().ToString("N");
        var email = !string.IsNullOrWhiteSpace(githubUser.Email) ? githubUser.Email : $"{name}@github.com";
        
        var user = new IdentityUser(Guid.NewGuid(), name, email);
        var createResult = await userManager.CreateAsync(user);
        
        if (!createResult.Succeeded)
        {
            _logger.LogError("User creation failed: {Errors}", 
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.SetRolesAsync(user, [AevatarPermissions.BasicUser]);
        
        return user;
    }
    
    private async Task<ClaimsPrincipal> CreateUserClaimsPrincipalAsync(ExtensionGrantContext context, IdentityUser user)
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
    
    private ForbidResult ErrorResult(string errorDescription)
    {
        return new ForbidResult(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = 
                    OpenIddictConstants.Errors.InvalidRequest,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = 
                    errorDescription
            }));
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context,
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
}