using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace Aevatar.AuthServer.Grants;

public class AppleGrantHandler : ITokenExtensionGrant, ITransientDependency
{
    public ILogger<AppleGrantHandler> Logger { get; set; }
    private readonly IAppleProvider _appleProvider;

    public string Name => GrantTypeConstants.APPLE;

    public AppleGrantHandler(IAppleProvider appleProvider)
    {
        _appleProvider = appleProvider;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        try
        {
            var code = context.Request.GetParameter("code")?.ToString();
            var idToken = context.Request.GetParameter("id_token")?.ToString();
            var source = context.Request.GetParameter("source")?.ToString();
            
            Logger.LogDebug("AppleGrantHandler.HandleAsync source: {source} idToken: {idToken} code: {code}", 
                source, idToken, code);
            
            if (string.IsNullOrEmpty(idToken))
            {
                if (string.IsNullOrEmpty(code))
                {
                    Logger.LogDebug("Missing both id_token and code");
                    return ErrorResult("Missing both id_token and code");
                }
                
                idToken = await _appleProvider.ExchangeCodeForTokenAsync(code, source);

                if (idToken.IsNullOrEmpty())
                {
                    return ErrorResult("Code invalid or expired");
                }
            }
            
            var (isValid, principal) = await _appleProvider.ValidateAppleTokenAsync(idToken, source);
            if (!isValid)
            {
                return ErrorResult("Invalid APPLE token");
            }

            var appleUser = ExtractAppleUser(principal);

            var email = appleUser.Email;
            Logger.LogDebug("AppleGrantHandler.HandleAsync: email: {email}", email);
            var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();

            var user = await userManager.FindByLoginAsync(GrantTypeConstants.APPLE, appleUser.SubjectId);
            if (user == null)
            {
                // Compatible with historical data login
                var name = email + "@" + GrantTypeConstants.APPLE;
                user = await userManager.FindByNameAsync(name);
                
                if (user == null && !string.IsNullOrWhiteSpace(email))
                {
                    user = await userManager.FindByEmailAsync(email);
                }
                
                if (user == null)
                {
                    name = Guid.NewGuid().ToString("N");
                    user = new IdentityUser(Guid.NewGuid(), name, email: email.IsNullOrWhiteSpace() ? $"{name}@apple.com":email);
                    await userManager.CreateAsync(user);
                    await userManager.SetRolesAsync(user,
                        [AevatarPermissions.BasicUser]);
                }
                
                await userManager.AddLoginAsync(user, new UserLoginInfo(
                    GrantTypeConstants.APPLE, 
                    appleUser.SubjectId, 
                    GrantTypeConstants.APPLE));
            }

            var identityRoleManager = context.HttpContext.RequestServices.GetRequiredService<IdentityRoleManager>();
            var roleNames = new List<string>();
            foreach (var userRole in user.Roles)
            {
                var role = await identityRoleManager.GetByIdAsync(userRole.RoleId);
                roleNames.Add(role.Name);
            }

            var userClaimsPrincipalFactory = context.HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<IdentityUser>>();
            var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
            claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
            claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Role, string.Join(",", roleNames));
            claimsPrincipal.SetScopes(context.Request.GetScopes());
            claimsPrincipal.SetResources(await GetResourcesAsync(context, claimsPrincipal.GetScopes()));
            claimsPrincipal.SetAudiences("Aevatar");
            await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
                .HandleAsync(context.Request, claimsPrincipal);

            return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "APPLE login failed");
            return ErrorResult("Internal server error");
        }
    }

    private AppleUserInfo ExtractAppleUser(ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname);
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (email?.EndsWith("@privaterelay.appleid.com") == true)
        {
            email = $"{sub}@apple.privaterelay.com";
        }

        return new AppleUserInfo
        {
            SubjectId = sub,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };
    }

    private ForbidResult ErrorResult(string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string?>
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