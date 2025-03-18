using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Aevatar.Controllers;

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string id_token { get; set; } = string.Empty;
}

[Route("api/account")]
public class AccountController : AevatarController
{
    private readonly ILogger<AccountController> _logger;
    
    public AccountController(
        ILogger<AccountController> logger)
    {
        _logger = logger;
    }
    
    
    [HttpGet("login-google")]
    public IActionResult ExternalLogin()
    {
        var redirectUrl = Url.Action("LoginCallback", "Account");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
    
    // [HttpPost("LoginGoogle")]
    // public async Task<IActionResult> LoginGoogle([FromForm] GoogleLoginRequest request)
    // {
    //     if (string.IsNullOrEmpty(request.IdToken))
    //     {
    //         return BadRequest(new { message = "Missing ID Token" });
    //     }
    //
    //     // Step 1
    //     var payload = await VerifyGoogleIdToken(request.IdToken);
    //     if (payload == null)
    //     {
    //         return Unauthorized(new { message = "Invalid ID Token" });
    //     }
    //
    //     // Step 2
    //     var email = payload.Email;
    //     var name = payload.Name;
    //
    //     // create local user
    //     var userManager = HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
    //     var user = await userManager.FindByNameAsync(email);
    //     if (user == null)
    //     {
    //         user = new IdentityUser(Guid.NewGuid(), email, email);
    //         await userManager.CreateAsync(user);
    //         await userManager.SetRolesAsync(user, new[] { "BasicUser" });
    //     }
    //
    //     // Step 3: create ClaimsPrincipal and return Token
    //     var userClaimsPrincipalFactory = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<IdentityUser>>();
    //     var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
    //
    //     await HttpContext.SignInAsync(
    //         IdentityConstants.ApplicationScheme,
    //         claimsPrincipal
    //     );
    //
    //     return Ok(new { message = "Login successful" });
    // }

    // private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
    // {
    //     try
    //     {
    //         var settings = new GoogleJsonWebSignature.ValidationSettings
    //         {
    //             Audience = new[] { _configuration["Google:ClientId"] }
    //         };
    //         return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    //     }
    //     catch
    //     {
    //         return null;
    //     }
    // }
    
    private async Task<Google.Apis.Auth.GoogleJsonWebSignature.Payload?> VerifyGoogleIdToken(string idToken)
    {
        try
        {
            // 使用 Google.Apis 验证 Google ID Token
            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken);
            return payload;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ID Token validation failed: {ex.Message}");
            return null;
        }
    }

    
    [HttpGet("LoginCallback")]
    public async Task<IActionResult> LoginCallback()
    {
        _logger.LogInformation("LoginCallback being");
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
        if (User.Identity.IsAuthenticated)
        {
            Console.WriteLine("User is authenticated.");
        }
        else
        {
            Console.WriteLine("User is not authenticated.");
        }
            
        var userManager = HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        
        var user = await userManager.FindByNameAsync(email);
        
        if (user == null)
        {
            _logger.LogInformation("LoginCallback Create User");
            user = new IdentityUser(Guid.NewGuid(), email, email: Guid.NewGuid().ToString("N") + "@ABP.IO");
            await userManager.CreateAsync(user);
            await userManager.SetRolesAsync(user,
                [AevatarPermissions.BasicUser]);
        }
        var identityUser = await userManager.FindByNameAsync(email);
        _logger.LogInformation("LoginCallback Found User");
        var identityRoleManager = HttpContext.RequestServices.GetRequiredService<IdentityRoleManager>();
        var roleNames = new List<string>();
        foreach (var userRole in identityUser.Roles)
        {
            var role = await identityRoleManager.GetByIdAsync(userRole.RoleId);
            roleNames.Add(role.Name);
        }
        
        var userClaimsPrincipalFactory = HttpContext.RequestServices
            .GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<IdentityUser>>();
        var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
        
        claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Email, email);
        claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Name, name);
        claimsPrincipal.SetClaim(OpenIddictConstants.Claims.Role, string.Join(",",roleNames));
        claimsPrincipal.SetAudiences("Aevatar");

        var uid = user.Id;
        
        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme, 
            claimsPrincipal);
        
        _logger.LogInformation("LoginCallback Redirect");
        return Redirect($"https://auth-station-staging.aevatar.ai/connect/authorize?response_type=code&client_id=AevatarAuthServer&redirect_uri=http://localhost:8001/rsedirect_page_after_login&scope=Aevatar");
        
        // return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
    
    
    // private async Task<string> GenerateAuthorizationCodeAsync(Guid userId)
    // {
    //     var authorizationManager = HttpContext.RequestServices
    //         .GetRequiredService<IOpenIddictAuthorizationManager>();
    //
    //     var authorization = await authorizationManager.CreateAsync(
    //         principal: User,
    //         subject: userId.ToString(),
    //         client: "your_client_id", 
    //         type: OpenIddictConstants.AuthorizationTypes.AdHoc,
    //         scopes: new[] { "openid", "profile", "email" }
    //     );
    //
    //     return authorization;
    // }
}