using System.Collections.Immutable;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace Aevatar.AuthServer.Grants;

public class SignatureGrantHandler : ITokenExtensionGrant, ITransientDependency
{
    public ILogger<SignatureGrantHandler> Logger { get; set; }
    private IWalletLoginProvider _walletLoginProvider;

    public string Name { get; } = GrantTypeConstants.SIGNATURE;

    public SignatureGrantHandler(IWalletLoginProvider walletLoginProvider)
    {
        _walletLoginProvider = walletLoginProvider;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var publicKeyVal = context.Request.GetParameter("pubkey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var chainId = context.Request.GetParameter("chain_id").ToString();
        var caHash = context.Request.GetParameter("ca_hash").ToString();
        var plainText = context.Request.GetParameter("plain_text").ToString();
        
        var errors = _walletLoginProvider.CheckParams(publicKeyVal, signatureVal, chainId, plainText);
        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        _walletLoginProvider.GetErrorMessage(errors)
                }!));
        }

        string walletAddress = string.Empty;
        try
        {
            walletAddress = await _walletLoginProvider.VerifySignatureAndParseWalletAddressAsync(publicKeyVal,
                signatureVal, plainText, caHash, chainId);
        }
        catch (UserFriendlyException verifyException)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                verifyException.Message);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[SignatureGrantHandler] Signature validation failed");
            throw;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        var user = await userManager.FindByNameAsync(walletAddress);
        if (user == null)
        {
            user = new IdentityUser(Guid.NewGuid(), walletAddress, email: Guid.NewGuid().ToString("N") + "@ABP.IO");
            await userManager.CreateAsync(user);
            await userManager.SetRolesAsync(user,
                [AevatarPermissions.BasicUser]);
        }
        else if (user.Roles.IsNullOrEmpty())
        {
            await userManager.SetRolesAsync(user,
                [AevatarPermissions.BasicUser]);
        }

        var identityUser = await userManager.FindByNameAsync(walletAddress);
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
        claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Role, string.Join(",", roleNames));
        claimsPrincipal.SetScopes(context.Request.GetScopes());
        claimsPrincipal.SetResources(await GetResourcesAsync(context, claimsPrincipal.GetScopes()));
        claimsPrincipal.SetAudiences("Aevatar");
        await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, claimsPrincipal);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }

    private ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
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