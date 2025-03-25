using System.Collections.Immutable;
using System.Security.Claims;
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

public class GoogleGrantHandler : ITokenExtensionGrant
{
    private readonly IConfiguration _configuration;
    private ILogger<GoogleGrantHandler> _logger;

    public string Name => GrantTypeConstants.GOOGLE;
    
    public GoogleGrantHandler(
        IConfiguration configuration, 
        ILogger<GoogleGrantHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var idToken = context.Request.GetParameter("id_token").ToString();
        var source = context.Request.GetParameter("source")?.ToString();
        
        _logger.LogInformation("GoogleGrantHandler.HandleAsync source: {source} idToken: {idToken}", source, idToken);
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

        string clientId;
        if (source == "ios")
        {
            clientId = _configuration["Google:IOSClientId"];
        }
        else if (source == "android")
        {
            clientId = _configuration["Google:AndroidClientId"];
        }
        else
        {
            clientId = _configuration["Google:WebClientId"];
        }
        
        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogInformation("GoogleGrantHandler.HandleAsync: clientId not found");
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
        
        _logger.LogInformation("GoogleGrantHandler.HandleAsync: clientId: {clientId}", clientId);
        
        var payload = await ValidateGoogleTokenAsync(idToken);
        _logger.LogInformation("GoogleGrantHandler.HandleAsync: payload: {payload}", payload);
        
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
        _logger.LogInformation("GoogleGrantHandler.HandleAsync: email: {email}", email);
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();

        var name = email + "@" + GrantTypeConstants.GOOGLE;
        var user = await userManager.FindByNameAsync(name);
        if (user == null)
        {
            user = new IdentityUser(Guid.NewGuid(), name, email: Guid.NewGuid().ToString("N") + "@ABP.IO");
            await userManager.CreateAsync(user);
            await userManager.SetRolesAsync(user,
                [AevatarPermissions.BasicUser]);
        }
        var identityUser = await userManager.FindByNameAsync(name);
        var identityRoleManager = context.HttpContext.RequestServices.GetRequiredService<IdentityRoleManager>();
        var roleNames = new List<string>();
        foreach (var userRole in identityUser.Roles)
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

    private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var clientId = _configuration["Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleGrantHandler.ValidateGoogleTokenAsync: {ex} msg {msg}", ex, ex.ToString());
            return null;
        }
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