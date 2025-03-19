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
    private readonly IdentityUserManager _userManager;
    private ILogger<GoogleGrantHandler> _logger;

    public string Name => GrantTypeConstants.GOOGLE;
    

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<GoogleGrantHandler>>();
        // 1. 获取Google ID Token
        var idToken = context.Request.GetParameter("id_token").ToString();
        _logger.LogInformation("GoogleGrantHandler.HandleAsync: idToken: {idToken}", idToken);
        if (string.IsNullOrEmpty(idToken))
        {
            return new ForbidResult(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = 
                        OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = 
                        "Missing id_token parameter"
                }));
        }

        // 2. 验证Google Token
        var payload = await ValidateGoogleTokenAsync(idToken);
        _logger.LogInformation("GoogleGrantHandler.HandleAsync: payload: {payload}", payload);
        if (payload == null)
        {
            return new ForbidResult(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = 
                        OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = 
                        "Invalid Google token"
                }));
        }

        // 3. 创建或获取本地用户（复用与Signature相同的逻辑）
        var email = payload.Email;
        // var user = await _userManager.FindByEmailAsync(email) 
        //     ?? await CreateUserAsync(email);
        _logger.LogInformation("GoogleGrantHandler.HandleAsync: email: {email}", email);
        
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();

        var user = await userManager.FindByNameAsync(email);
        if (user == null)
        {
            user = new IdentityUser(Guid.NewGuid(), email, email: Guid.NewGuid().ToString("N") + "@ABP.IO");
            await userManager.CreateAsync(user);
            await userManager.SetRolesAsync(user,
                [AevatarPermissions.BasicUser]);
        }
        var identityUser = await userManager.FindByNameAsync(email);
        var identityRoleManager = context.HttpContext.RequestServices.GetRequiredService<IdentityRoleManager>();
        var roleNames = new List<string>();
        foreach (var userRole in identityUser.Roles)
        {
            var role = await identityRoleManager.GetByIdAsync(userRole.RoleId);
            roleNames.Add(role.Name);
        }

        // 4. 生成ClaimsPrincipal（保持与Signature相同）
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
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleGrantHandler.ValidateGoogleTokenAsync: {ex} msg {msg}", ex, ex.ToString());
            return null;
        }
    }

    private async Task<IdentityUser> CreateUserAsync(string email)
    {
        var user = new IdentityUser(Guid.NewGuid(), email, email: email);
        await _userManager.CreateAsync(user);
        await _userManager.AddToRoleAsync(user, AevatarPermissions.BasicUser);
        return user;
    }

    // private async Task<ClaimsPrincipal> CreateUserPrincipalAsync(IdentityUser user)
    // {
    //     var principal = await _userManager.CreateUserPrincipalAsync(user);
    //     principal.SetScopes(context.Request.GetScopes());
    //     principal.SetResources(await GetResourcesAsync(context.Request.Scopes));
    //     return principal;
    // }
    
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