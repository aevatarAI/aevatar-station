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

public class SignatureGrantHandler : GrantHandlerBase, ITransientDependency
{
    private readonly ILogger<SignatureGrantHandler> _logger;
    private IWalletLoginProvider _walletLoginProvider;

    public override string Name { get; } = GrantTypeConstants.SIGNATURE;

    public SignatureGrantHandler(IWalletLoginProvider walletLoginProvider, ILogger<SignatureGrantHandler> logger)
    {
        _walletLoginProvider = walletLoginProvider;
        _logger = logger;
    }

    public override async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var publicKeyVal = context.Request.GetParameter("pubkey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var chainId = context.Request.GetParameter("chain_id").ToString();
        var caHash = context.Request.GetParameter("ca_hash").ToString();
        var plainText = context.Request.GetParameter("plain_text").ToString();
        
        var errors = _walletLoginProvider.CheckParams(publicKeyVal, signatureVal, chainId, plainText);
        if (errors.Count > 0)
        {
            return CreateForbidResult(_walletLoginProvider.GetErrorMessage(errors));
        }

        string walletAddress = string.Empty;
        try
        {
            walletAddress = await _walletLoginProvider.VerifySignatureAndParseWalletAddressAsync(publicKeyVal,
                signatureVal, plainText, caHash, chainId);
        }
        catch (UserFriendlyException verifyException)
        {
            return CreateForbidResult(verifyException.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[SignatureGrantHandler] Signature validation failed");
            throw;
        }

        var isNewUser = false;
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        var user = await userManager.FindByNameAsync(walletAddress);
        if (user == null)
        {
            isNewUser = true;
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
        var claimsPrincipal = await CreateUserClaimsPrincipalWithFactoryAsync(context, identityUser, isNewUser);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }
} 