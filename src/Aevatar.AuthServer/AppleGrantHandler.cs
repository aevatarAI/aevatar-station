using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Aevatar.Constants;
using Aevatar.Options;
using Microsoft.Extensions.Options;
using Namotion.Reflection;

namespace Aevatar;

public class AppleGrantHandler : ITokenExtensionGrant, ITransientDependency
{
    private readonly ILogger<AppleGrantHandler> _logger;

    public string Name => GrantTypeConstants.APPLE;
    private const string PlatFormMobile = "mobile";

    public AppleGrantHandler(
        IConfiguration configuration,
        ILogger<AppleGrantHandler> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        try
        {
            var code = context.Request.GetParameter("code")?.ToString();
            var idToken = context.Request.GetParameter("id_token")?.ToString();
            var source = context.Request.GetParameter("source")?.ToString();
            var platform = context.Request.GetParameter("platform")?.ToString() ?? string.Empty;
            var clientId = context.Request.GetParameter("client_id")?.ToString();
            
            _logger.LogInformation("AppleGrantHandler.HandleAsync source: {source} idToken: {idToken} code: {code} platform: {platform} clientId: {clientId}", 
                source, idToken, code, platform, clientId);

            var appleOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<AppleOptions>>();
            if (!appleOptions.CurrentValue.APPs.TryGetValue(clientId, out var appOptions))
            {
                _logger.LogInformation("Missing both id_token and code");
                return ErrorResult("Missing both id_token and code");
            }
            
            var aud = source == "ios" ? appOptions.NativeClientId : appOptions.WebClientId;
            if (string.IsNullOrEmpty(idToken))
            {
                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogInformation("Missing both id_token and code");
                    return ErrorResult("Missing both id_token and code");
                }
                
                idToken = await ExchangeCodeForTokenAsync(code, aud, platform, appOptions);
                _logger.LogInformation("AppleGrantHandler.HandleAsync ExchangeCodeForTokenAsync code: {idToken} aud: {aud} token: {token}", code, aud, idToken);

                if (idToken.IsNullOrEmpty())
                {
                    return ErrorResult("Code invalid or expired");
                }
            }
            
            var (isValid, principal) = await ValidateAppleToken(idToken, aud);
            if (!isValid)
            {
                return ErrorResult("Invalid APPLE token");
            }

            var appleUser = ExtractAppleUser(principal);

            var email = appleUser.Email;
            _logger.LogInformation("AppleGrantHandler.HandleAsync: email: {email}", email);
            var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();

            var name = email + "@" + GrantTypeConstants.APPLE;
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
            _logger.LogError(ex, "APPLE login failed");
            return ErrorResult("Internal server error");
        }
    }

    private async Task<(bool IsValid, ClaimsPrincipal? Principal)> ValidateAppleToken(string idToken, string audience)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var jwtToken = tokenHandler.ReadJwtToken(idToken);
            var kid = jwtToken.Header[AppleConstants.Claims.Kid]?.ToString();
            var aud = jwtToken.Audiences.FirstOrDefault();
            
            _logger.LogInformation("AppleGrantHandler.ValidateAppleToken: kid: {kid} required aud: {audience} actual aud: {aud}", 
                kid, audience, aud);

            var key = await GetApplePublicKeysAsync(kid);
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = AppleConstants.ValidIssuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(idToken, validationParameters, out _);
            return (true, principal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "APPLE token validation failed");
            _logger.LogError( "AppleGrantHandler.ValidateAppleToken failed, msg: {msg}", ex.Message);
            return (false, null);
        }
    }
    
    private async Task<SecurityKey> GetApplePublicKeysAsync(string kid)
    {
        using var client = new HttpClient();
        var keysResponse = await client.GetStringAsync(AppleConstants.JwksEndpoint);
        var keys = JObject.Parse(keysResponse)["keys"];

        foreach (var key in keys)
        {
            if (key["kid"]?.ToString() == kid)
            {
                var modulus = Base64UrlEncoder.DecodeBytes(key["n"].ToString());
                var exponent = Base64UrlEncoder.DecodeBytes(key["e"].ToString());
                
                var rsaParameters = new RSAParameters
                {
                    Modulus = modulus,
                    Exponent = exponent
                };
                
                return new RsaSecurityKey(rsaParameters);
            }
        }

        throw new SecurityTokenException($"No public key found for kid: {kid}");
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
    
    private async Task<string> ExchangeCodeForTokenAsync(string code, string clientId, string platform,
        AppleAppOptions appOptions)
    {
        var clientSecret = GenerateClientSecret(clientId, appOptions);
        using var client = new HttpClient();

        var redirectUrl = string.Empty;
        if (platform == PlatFormMobile)
        {
            redirectUrl = appOptions.MobileRedirectUri;
        }
        else
        {
            redirectUrl = appOptions.RedirectUri;
        }
        
        var body = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUrl),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
        };
        
        var response = await client.PostAsync(AppleConstants.TokenEndpoint, new FormUrlEncodedContent(body));
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Token exchange failed. StatusCode: {StatusCode}, Response: {ResponseBody}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync());
            return "";
        }
        
        var json = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("[AppleGrantHandler][ExchangeCodeForTokenAsync]  json: {json}",json);

        var tokenResp = JsonConvert.DeserializeObject<TokenResponse>(json);
        return tokenResp.IdToken;
    }
    
    private string GenerateClientSecret(string clientId, AppleAppOptions appOptions)
    {
        var key =  Regex.Replace(appOptions.Pk, @"\t|\n|\r", "");
        using var algorithm = ECDsa.Create();
        algorithm.ImportPkcs8PrivateKey(Convert.FromBase64String(key), out _);

        var now = DateTime.UtcNow;
        var header = new
        {
            alg = "ES256", 
            kid = appOptions.KeyId  
        };
        
        var payload = new
        {
            iss = appOptions.TeamId, 
            iat = new DateTimeOffset(now).ToUnixTimeSeconds(), 
            exp = new DateTimeOffset(now.AddMinutes(30)).ToUnixTimeSeconds(),
            aud = "https://appleid.apple.com", 
            sub = clientId 
        };
        
        var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(header)));
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
        
        var stringToSign = $"{encodedHeader}.{encodedPayload}";
        var signature = algorithm.SignData(Encoding.UTF8.GetBytes(stringToSign), HashAlgorithmName.SHA256);
        var encodedSignature = Base64UrlEncode(signature);
       
        return $"{encodedHeader}.{encodedPayload}.{encodedSignature}";
    }
    
    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
    
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("id_token")]
        public string? IdToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }
    }

    private class AppleUserInfo
    {
        public string SubjectId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}