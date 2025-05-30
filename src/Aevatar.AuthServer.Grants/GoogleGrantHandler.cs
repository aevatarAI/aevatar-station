using System.Collections.Immutable;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace Aevatar.AuthServer.Grants;

public class GoogleGrantHandler : ITokenExtensionGrant
{
    private readonly ILogger<GoogleGrantHandler> _logger;
    private readonly IGoogleProvider _googleProvider;

    public string Name => GrantTypeConstants.GOOGLE;
    
    public GoogleGrantHandler(IGoogleProvider googleProvider, ILogger<GoogleGrantHandler> logger)
    {
        _googleProvider = googleProvider;
        _logger = logger;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var idToken = context.Request.GetParameter("id_token").ToString();
        var source = context.Request.GetParameter("source")?.ToString();
        
        _logger.LogDebug("GoogleGrantHandler.HandleAsync source: {source} idToken: {idToken}", source, idToken);
        if (string.IsNullOrEmpty(idToken))
        {
            return new ForbidResult(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = 
                        OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = 
                        "Missing id_token parameter"
                }));
        }

        var clientId = await _googleProvider.GetClientIdAsync(source);
        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogDebug("GoogleGrantHandler.HandleAsync: clientId not found");
            return new ForbidResult(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            OpenIddictConstants.Errors.InvalidRequest,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "client Id not found"
                    }
                )
            );
        }
        
        _logger.LogDebug("GoogleGrantHandler.HandleAsync: clientId: {clientId}", clientId);
        
        var payload = await _googleProvider.ValidateGoogleTokenAsync(idToken, clientId);
        _logger.LogDebug("GoogleGrantHandler.HandleAsync: payload: {payload}", payload);
        
        if (payload == null)
        {
            return new ForbidResult(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = 
                        OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = 
                        "Invalid Google token"
                }));
        }
        
        var email = payload.Email;
        _logger.LogDebug("GoogleGrantHandler.HandleAsync: email: {email}", email);
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();

        var user = await userManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, payload.Subject);
        if (user == null)
        {
            // Compatible with historical data login
            var name = email + "@" + GrantTypeConstants.GOOGLE;
            user = await userManager.FindByNameAsync(name);

            if (user == null && !string.IsNullOrWhiteSpace(email))
            {
                user = await userManager.FindByEmailAsync(email);
            }

            if (user == null)
            {
                name = Guid.NewGuid().ToString("N");
                user = new IdentityUser(Guid.NewGuid(), name,
                    email: email.IsNullOrWhiteSpace() ? $"{name}@google.com" : email);
                await userManager.CreateAsync(user);
                await userManager.SetRolesAsync(user,
                    [AevatarPermissions.BasicUser]);
            }
            
            await userManager.AddLoginAsync(user, new UserLoginInfo(
                GrantTypeConstants.GOOGLE, 
                payload.Subject, 
                GrantTypeConstants.GOOGLE));
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
        claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Role, string.Join(",",roleNames));
        claimsPrincipal.SetScopes(context.Request.GetScopes());
        claimsPrincipal.SetResources(await GetResourcesAsync(context, claimsPrincipal.GetScopes()));
        claimsPrincipal.SetAudiences("Aevatar");
        await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, claimsPrincipal);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
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