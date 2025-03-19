using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.OpenIddict;

public class AppleGrantHandler : ITokenExtensionGrant, ITransientDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IdentityUserManager _userManager;
    private readonly ILogger<AppleGrantHandler> _logger;

    public string Name => "apple_login";

    public AppleGrantHandler(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        IdentityUserManager userManager,
        ILogger<AppleGrantHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        try
        {
            // 1. 获取认证参数
            var code = context.Request.GetParameter("code")?.ToString();
            var idToken = context.Request.GetParameter("id_token")?.ToString();

            if (string.IsNullOrEmpty(idToken) // 优先使用id_token
            {
                if (string.IsNullOrEmpty(code))
                {
                    return ErrorResult("Missing both id_token and code");
                }
                
                // 使用code换取id_token（需要实现）
                idToken = await ExchangeCodeForToken(code);
            }

            // 2. 验证Apple身份令牌
            var (isValid, principal) = await ValidateAppleToken(idToken);
            if (!isValid)
            {
                return ErrorResult("Invalid Apple token");
            }

            // 3. 提取用户信息
            var appleUser = ExtractAppleUser(principal);

            var email = appleUser.Email;
            _logger.LogInformation("AppleGrantHandler.HandleAsync: email: {email}", email);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple login failed");
            return ErrorResult("Internal server error");
        }
    }

    #region Private Methods
    
    private async Task<string> ExchangeCodeForToken(string code)
    {
        // 实现使用code换取id_token的逻辑（需要Apple服务端密钥）
        throw new NotImplementedException("Code exchange not implemented");
    }

    private async Task<(bool IsValid, ClaimsPrincipal Principal)> ValidateAppleToken(string idToken)
    {
        try
        {
            var keys = await GetApplePublicKeysAsync();
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = "https://appleid.apple.com",
                ValidAudience = _configuration["Apple:ClientId"],
                IssuerSigningKeys = keys,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidateIssuer = true
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(idToken, validationParameters, out _);
            return (true, principal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Apple token validation failed");
            return (false, null);
        }
    }

    private async Task<IEnumerable<SecurityKey>> GetApplePublicKeysAsync()
    {
        const string cacheKey = "ApplePublicKeys";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // 缓存10分钟
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://appleid.apple.com/auth/keys");
            var json = await response.Content.ReadAsStringAsync();
            
            var jwks = JsonConvert.DeserializeObject<JsonWebKeySet>(json);
            return jwks.Keys;
        });
    }

    private AppleUserInfo ExtractAppleUser(ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname);
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        // 处理Apple的匿名邮箱
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

    private async Task<IdentityUser> FindOrCreateUserAsync(AppleUserInfo appleUser)
    {
        var user = await _userManager.FindByLoginAsync("Apple", appleUser.SubjectId);
        if (user != null) return user;

        // 创建新用户
        user = new IdentityUser(
            GuidGenerator.Create(),
            userName: appleUser.Email ?? GenerateTemporaryUsername(),
            email: appleUser.Email
        )
        {
            Name = $"{appleUser.FirstName} {appleUser.LastName}".Trim(),
            EmailConfirmed = appleUser.Email != null
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException($"User creation failed: {string.Join(",", result.Errors)}");
        }

        // 添加外部登录
        await _userManager.AddLoginAsync(user, new UserLoginInfo(
            loginProvider: "Apple",
            providerKey: appleUser.SubjectId,
            displayName: "Apple"
        ));

        return user;
    }

    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(
        IdentityUser user, 
        ExtensionGrantContext context)
    {
        var principal = await _userManager.CreateUserPrincipalAsync(user);
        
        // 添加OpenIddict所需声明
        principal.SetScopes(context.Request.GetScopes());
        principal.SetResources(await GetResourcesAsync(context));
        principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());

        return principal;
    }

    private string GenerateTemporaryUsername()
    {
        return $"apple_user_{Guid.NewGuid():N}";
    }

    private ForbidResult ErrorResult(string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = 
                    OpenIddictConstants.Errors.InvalidRequest,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = 
                    errorDescription
            }));
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context)
    {
        var scopeManager = context.HttpContext.RequestServices
            .GetRequiredService<IOpenIddictScopeManager>();
        
        var resources = new List<string>();
        await foreach (var resource in scopeManager.ListResourcesAsync(context.Request.GetScopes()))
        {
            resources.Add(resource);
        }
        return resources;
    }

    #endregion

    #region Helper Classes

    private class AppleUserInfo
    {
        public string SubjectId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    #endregion
}