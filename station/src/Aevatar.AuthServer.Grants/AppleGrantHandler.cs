using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Aevatar.AuthServer.Grants.Options;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

public class AppleGrantHandler : GrantHandlerBase, ITransientDependency
{
    private readonly ILogger<AppleGrantHandler> _logger;
    private readonly IAppleProvider _appleProvider;

    public override string Name => GrantTypeConstants.APPLE;

    public AppleGrantHandler(IAppleProvider appleProvider, ILogger<AppleGrantHandler> logger)
    {
        _appleProvider = appleProvider;
        _logger = logger;
    }

    public override async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        try
        {
            var code = context.Request.GetParameter("code")?.ToString();
            var idToken = context.Request.GetParameter("id_token")?.ToString();
            var source = context.Request.GetParameter("source")?.ToString();
            var platform = context.Request.GetParameter("platform")?.ToString() ?? string.Empty;
            var appId = context.Request.GetParameter("apple_app_id")?.ToString();
            
            if (appId.IsNullOrWhiteSpace())
            {
                // TODO: Should not be here
                appId = "com.gpt.god";
            }
            
            _logger.LogDebug("AppleGrantHandler.HandleAsync source: {source} idToken: {idToken} code: {code} platform: {platform} clientId: {clientId}", 
                source, idToken, code, platform, appId);

            var appleOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<AppleOptions>>();
            if (!appleOptions.CurrentValue.APPs.TryGetValue(appId, out var appOptions))
            {
                _logger.LogInformation("Invalid apple_app_id ");
                return CreateForbidResult("Invalid apple_app_id");
            }
            
            if (string.IsNullOrEmpty(idToken))
            {
                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogDebug("Missing both id_token and code");
                    return CreateForbidResult("Missing both id_token and code");
                }
                
                idToken = await _appleProvider.ExchangeCodeForTokenAsync(code, source, platform, appOptions);

                if (idToken.IsNullOrEmpty())
                {
                    return CreateForbidResult("Code invalid or expired");
                }
            }
            
            var (isValid, principal) = await _appleProvider.ValidateAppleTokenAsync(idToken, source, appOptions);
            if (!isValid)
            {
                return CreateForbidResult("Invalid APPLE token");
            }

            var appleUser = ExtractAppleUser(principal);

            var email = appleUser.Email;
            _logger.LogDebug("AppleGrantHandler.HandleAsync: email: {email}", email);
            var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();

            var isNewUser = false;
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
                    isNewUser = true;
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

            var claimsPrincipal = await CreateUserClaimsPrincipalWithFactoryAsync(context, user, isNewUser);

            return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "APPLE login failed");
            return CreateForbidResult("Internal server error");
        }
    }

    private AppleUserInfo ExtractAppleUser(ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname);
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (email.IsNullOrWhiteSpace() || email.EndsWith("@privaterelay.appleid.com"))
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
}