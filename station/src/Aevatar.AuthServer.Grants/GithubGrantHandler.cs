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

public class GithubGrantHandler : GrantHandlerBase
{
    private readonly ILogger<GithubGrantHandler> _logger;
    private readonly IGithubProvider _githubProvider;

    public override string Name => GrantTypeConstants.Github;
    
    public GithubGrantHandler(IGithubProvider githubProvider, ILogger<GithubGrantHandler> logger)
    {
        _githubProvider = githubProvider;
        _logger = logger;
    }

    public override async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var code = context.Request.GetParameter("code").ToString();
        
        if (string.IsNullOrEmpty(code))
        {
            return CreateForbidResult("Missing code parameter");
        }

        var githubUser = await _githubProvider.GetUserInfoAsync(code);
        if (githubUser == null)
        {
            return CreateForbidResult("Invalid code");
        }
        
        var user = await GetOrCreateUserAsync(context, githubUser);
        if (user == null)
        {
            return CreateForbidResult("Failed to create or retrieve user");
        }
        
        var claimsPrincipal = await CreateUserClaimsPrincipalWithSignInManagerAsync(context, user);

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
} 