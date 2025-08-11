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

public class GoogleGrantHandler : GrantHandlerBase
{
    private readonly ILogger<GoogleGrantHandler> _logger;
    private readonly IGoogleProvider _googleProvider;

    public override string Name => GrantTypeConstants.GOOGLE;
    
    public GoogleGrantHandler(IGoogleProvider googleProvider, ILogger<GoogleGrantHandler> logger)
    {
        _googleProvider = googleProvider;
        _logger = logger;
    }

    public override async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var idToken = context.Request.GetParameter("id_token").ToString();
        var source = context.Request.GetParameter("source")?.ToString();
        
        _logger.LogDebug("GoogleGrantHandler.HandleAsync source: {source} idToken: {idToken}", source, idToken);
        if (string.IsNullOrEmpty(idToken))
        {
            return CreateForbidResult("Missing id_token parameter");
        }

        var clientId = await _googleProvider.GetClientIdAsync(source);
        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogDebug("GoogleGrantHandler.HandleAsync: clientId not found");
            return CreateForbidResult("client Id not found");
        }
        
        _logger.LogDebug("GoogleGrantHandler.HandleAsync: clientId: {clientId}", clientId);
        
        var payload = await _googleProvider.ValidateGoogleTokenAsync(idToken, clientId);
        _logger.LogDebug("GoogleGrantHandler.HandleAsync: payload: {payload}", payload);
        
        if (payload == null)
        {
            return CreateForbidResult(OpenIddictConstants.Errors.InvalidGrant, "Invalid Google token");
        }
        
        var email = payload.Email;
        _logger.LogDebug("GoogleGrantHandler.HandleAsync: email: {email}", email);
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();

        var isNewUser = false;
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
                isNewUser = true;
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
        
        var claimsPrincipal = await CreateUserClaimsPrincipalWithFactoryAsync(context, user, isNewUser);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }
}