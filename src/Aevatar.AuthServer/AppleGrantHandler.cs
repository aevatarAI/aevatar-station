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
using Aevatar.OpenIddict;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Volo.Abp.OpenIddict;

public class AppleGrantHandler : ITokenExtensionGrant, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppleGrantHandler> _logger;

    public string Name => GrantTypeConstants.APPLE;

    public AppleGrantHandler(
        IConfiguration configuration,
        ILogger<AppleGrantHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        try
        {
            var code = context.Request.GetParameter("code")?.ToString();
            var idToken = context.Request.GetParameter("id_token")?.ToString();
            var source = context.Request.GetParameter("source")?.ToString();
            
            _logger.LogInformation("AppleGrantHandler.HandleAsync source: {source} idToken: {idToken}", 
                source, idToken);

            if (string.IsNullOrEmpty(idToken))
            {
                if (string.IsNullOrEmpty(code))
                {
                    return ErrorResult("Missing both id_token and code");
                }

                idToken = await ExchangeCodeForToken(code);
            }

            var aud = source == "ios" ? _configuration["Apple:NativeClientId"] : _configuration["Apple:WebClientId"];
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

    private async Task<string> ExchangeCodeForToken(string code)
    {
        throw new NotImplementedException("Code exchange not implemented");
    }

    private async Task<(bool IsValid, ClaimsPrincipal Principal)> ValidateAppleToken(string idToken, string audience)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var jwtToken = tokenHandler.ReadJwtToken(idToken);
            var kid = jwtToken.Header["kid"]?.ToString();
            var aud = jwtToken.Audiences.FirstOrDefault();
            
            _logger.LogInformation("AppleGrantHandler.ValidateAppleToken: kid: {kid} required aud: {audience} actual aud: {aud}", 
                kid, audience, aud);

            var key = await NewGetApplePublicKeysAsync(kid);
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = "https://appleid.apple.com",
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
    

    private async Task<SecurityKey> NewGetApplePublicKeysAsync(string kid)
    {
        using var client = new HttpClient();
        var keysResponse = await client.GetStringAsync("https://appleid.apple.com/auth/keys");
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

    private string GenerateClientSecret()
    {
        var privateKey = File.ReadAllText("_privateKeyPath"); 
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey.ToCharArray());

        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa) { KeyId = "_keyId" },
            SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "https://appleid.apple.com", 
            Issuer = "_teamId", 
            Expires = now.AddMinutes(30), 
            NotBefore = now,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "_clientId") 
            }),
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.CreateEncodedJwt(securityTokenDescriptor);
    }
    

    private class AppleUserInfo
    {
        public string SubjectId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}