using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Aevatar.AuthServer.Grants.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace Aevatar.AuthServer.Grants.Providers;

public class AppleProvider : IAppleProvider, ITransientDependency
{
    private readonly ILogger<AppleProvider> _logger;
    private const string PlatFormMobile = "mobile";

    public AppleProvider(IConfiguration configuration, ILogger<AppleProvider> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, string source, string platform,
        AppleAppOptions appOptions)
    {
        try
        {
            var aud = source == "ios" ? appOptions.NativeClientId : appOptions.WebClientId;
            var clientSecret = GenerateClientSecret(aud, appOptions);
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
                new("grant_type", "authorization_code"),
                new("code", code),
                new("redirect_uri", redirectUrl),
                new("client_id", aud),
                new("client_secret", clientSecret),
            };
            
            var response = await client.PostAsync(AppleConstants.TokenEndpoint, new FormUrlEncodedContent(body));
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed. StatusCode: {StatusCode}, Response: {ResponseBody}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return "";
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var tokenResp = JsonConvert.DeserializeObject<TokenResponse>(json);
            return tokenResp?.IdToken ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AppleProvider.ExchangeCodeForTokenAsync failed");
            return "";
        }
    }

    public async Task<(bool IsValid, ClaimsPrincipal? Principal)> ValidateAppleTokenAsync(string idToken, string source,
        AppleAppOptions appOptions)
    {
        try
        {
            var audience = source == "ios" ? appOptions.NativeClientId : appOptions.WebClientId;
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(idToken);
            var kid = jwtToken.Header[AppleConstants.Claims.Kid]?.ToString();
            var aud = jwtToken.Audiences.FirstOrDefault();
            
            _logger.LogDebug("AppleProvider.ValidateAppleTokenAsync: kid: {kid} required aud: {audience} actual aud: {aud}", 
                kid, audience, aud);

            var key = await GetApplePublicKeysAsync(kid!);
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
            _logger.LogError(ex, "Apple token validation failed");
            return (false, null);
        }
    }

    private async Task<SecurityKey> GetApplePublicKeysAsync(string kid)
    {
        using var client = new HttpClient();
        var keysResponse = await client.GetStringAsync(AppleConstants.JwksEndpoint);
        var keys = JObject.Parse(keysResponse)["keys"];

        foreach (var key in keys!)
        {
            if (key["kid"]?.ToString() == kid)
            {
                var modulus = Base64UrlEncoder.DecodeBytes(key["n"]!.ToString());
                var exponent = Base64UrlEncoder.DecodeBytes(key["e"]!.ToString());
                
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

    private string GenerateClientSecret(string clientId, AppleAppOptions appOptions)
    {
        var key = Regex.Replace(appOptions.Pk, @"\t|\n|\r", "");
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
} 