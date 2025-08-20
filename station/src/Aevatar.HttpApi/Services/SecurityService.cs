using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Aevatar.Common;
using Aevatar.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aevatar.Services;

/// <summary>
/// Unified security service implementation
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    private readonly RecaptchaOptions _recaptchaOptions;
    private readonly AppleDeviceCheckOptions _appleOptions;
    private readonly PlayIntegrityOptions _playOptions;
    private readonly RateOptions _rateOptions;
    private readonly ILogger<SecurityService> _logger;
    
    // Static flag to ensure configuration is logged only once
    private static bool _hasLoggedConfiguration = false;

    public SecurityService(
        IDistributedCache cache,
        HttpClient httpClient,
        IOptions<RecaptchaOptions> recaptchaOptions,
        IOptions<AppleDeviceCheckOptions> appleOptions,
        IOptions<PlayIntegrityOptions> playOptions,
        IOptions<RateOptions> rateOptions,
        ILogger<SecurityService> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _recaptchaOptions = recaptchaOptions.Value;
        _appleOptions = appleOptions.Value;
        _playOptions = playOptions.Value;
        _rateOptions = rateOptions.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        // Log configuration only once at first instance creation (reduce log noise)
        if (!_hasLoggedConfiguration)
        {
            _hasLoggedConfiguration = true;
            _logger.LogWarning("SecurityService Configuration Debug - EnableRecaptcha={EnableRecaptcha}, EnableRateLimit={EnableRateLimit}, FreeRequestsPerDay={FreeRequestsPerDay}",
                _recaptchaOptions.Enabled, _rateOptions.Enabled, _rateOptions.FreeRequestsPerDay);
                
            _logger.LogWarning("SecurityService Configuration Details - SecretKey length={SecretKeyLength}",
                _recaptchaOptions.SecretKey?.Length ?? 0);
                
            // Check if we might still have old configuration keys
            if (_recaptchaOptions.Enabled == false && (_recaptchaOptions.SecretKey?.Length ?? 0) == 0)
            {
                _logger.LogError("SecurityService Configuration Issue - Both EnableRecaptcha=false and SecretKey is empty. Check config: Recaptcha.Enabled and Recaptcha.SecretKey");
            }
        }
    }

    #region IP Address Handling

    public string GetRealClientIp(HttpContext context)
    {
        // 1. Check X-Forwarded-For header (highest priority)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                var firstIp = ips[0].Trim();
                if (IsValidIpAddress(firstIp))
                {
                    _logger.LogDebug("Retrieved IP from X-Forwarded-For: {ip}", firstIp);
                    return firstIp;
                }
            }
        }

        // 2. Check X-Real-IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            var ip = realIp.ToString().Trim();
            if (IsValidIpAddress(ip))
            {
                _logger.LogDebug("Retrieved IP from X-Real-IP: {ip}", ip);
                return ip;
            }
        }

        // 3. Use connection remote IP
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _logger.LogDebug("Retrieved IP from RemoteIpAddress: {ip}", remoteIp);
        return remoteIp;
    }

    private bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

    #endregion

    #region Request Count and Rate Limiting

    public async Task<bool> IsSecurityVerificationRequiredAsync(string clientIp)
    {
        if (!_rateOptions.Enabled)
        {
            return false;
        }

        var count = await GetCurrent10MinutesRequestCountAsync(clientIp);
        var required = count > _rateOptions.FreeRequestsPerDay;

        _logger.LogDebug("IP {clientIp} current 10-minute window request count: {count}, verification required: {required}",
            clientIp, count, required);

        return required;
    }

    public async Task<int> IncrementRequestCountAsync(string clientIp)
    {
        var key = GetCacheKey(clientIp);
        var expiry = GetExpiryTime();
        
        // Try to increment atomically, if key doesn't exist, create it with value 1
        var newCount = await IncrementAtomicallyAsync(key, expiry);

        _logger.LogDebug("IP {clientIp} request count incremented to: {count}", clientIp, newCount);
        return newCount;
    }

    private async Task<int> IncrementAtomicallyAsync(string key, DateTimeOffset expiry)
    {
        try
        {
            // Try to get current value and increment atomically
            var currentValueStr = await _cache.GetStringAsync(key);
            
            if (currentValueStr == null)
            {
                // Key doesn't exist, try to create it with value 1
                var success = await TrySetIfNotExistsAsync(key, "1", expiry);
                if (success)
                {
                    return 1;
                }
                // If creation failed, someone else created it, read current value and continue
                currentValueStr = await _cache.GetStringAsync(key) ?? "0";
            }

            if (int.TryParse(currentValueStr, out var currentValue))
            {
                var newValue = currentValue + 1;
                // Update with new value and expiry
                await _cache.SetStringAsync(key, newValue.ToString(),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = expiry
                    });
                return newValue;
            }
            else
            {
                // Invalid value, reset to 1
                await _cache.SetStringAsync(key, "1",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = expiry
                    });
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing request count for key {key}", key);
            // In case of error, assume count as 1 to be safe
            return 1;
        }
    }

    private async Task<bool> TrySetIfNotExistsAsync(string key, string value, DateTimeOffset expiry)
    {
        try
        {
            // Simple implementation: try to set and check if it was us who set it
            await _cache.SetStringAsync(key, value,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = expiry
                });
            
            // Verify it was set correctly
            var verifyValue = await _cache.GetStringAsync(key);
            return verifyValue == value;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> GetCurrent10MinutesRequestCountAsync(string clientIp)
    {
        var key = GetCacheKey(clientIp);
        var countStr = await _cache.GetStringAsync(key);

        if (int.TryParse(countStr, out var count))
        {
            return count;
        }

        return 0;
    }

    private string GetCacheKey(string clientIp)
    {
        // Generate cache key based on 10-minute windows
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 10) * 10, 0);
        var windowKey = windowStart.ToString("yyyyMMddHHmm");
        var cacheKey = $"SendRegCode:{clientIp}:{windowKey}";
        
        _logger.LogDebug("Cache key calculation - IP: {clientIp}, Now: {now}, Window: {windowStart}-{windowEnd}, Key: {key}", 
            clientIp,
            now.ToString("yyyy-MM-dd HH:mm:ss"), 
            windowStart.ToString("yyyy-MM-dd HH:mm:ss"), 
            windowStart.AddMinutes(10).ToString("yyyy-MM-dd HH:mm:ss"),
            cacheKey);
            
        return cacheKey;
    }

    private DateTimeOffset GetExpiryTime()
    {
        // Set expiry to end of current 10-minute window
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 10) * 10, 0);
        var windowEnd = windowStart.AddMinutes(10);
        return new DateTimeOffset(windowEnd);
    }

    #endregion

    #region Security Verification

    public async Task<SecurityVerificationResult> VerifySecurityAsync(SecurityVerificationRequest request)
    {
        return request.Platform switch
        {
            PlatformType.Web => await VerifyWebSecurityAsync(request),
            PlatformType.iOS => await VerifyiOSSecurityAsync(request),
            PlatformType.Android => await VerifyAndroidSecurityAsync(request),
            _ => SecurityVerificationResult.CreateFailure("Unsupported platform type")
        };
    }

    private async Task<SecurityVerificationResult> VerifyWebSecurityAsync(SecurityVerificationRequest request)
    {
        _logger.LogInformation("Web security verification: EnableRecaptcha={enabled}, HasToken={hasToken}", 
            _recaptchaOptions.Enabled, !string.IsNullOrEmpty(request.RecaptchaToken));
            
        if (!_recaptchaOptions.Enabled)
        {
            _logger.LogWarning("Web reCAPTCHA verification disabled, skipping verification - this allows bypass!");
            return SecurityVerificationResult.CreateSuccess("reCAPTCHA (disabled)");
        }

        if (string.IsNullOrEmpty(request.RecaptchaToken))
        {
            _logger.LogWarning("Missing reCAPTCHA token for web platform verification");
            return SecurityVerificationResult.CreateFailure("Missing reCAPTCHA verification token");
        }

        var isValid = await VerifyRecaptchaAsync(request.RecaptchaToken, request.ClientIp);
        return isValid
            ? SecurityVerificationResult.CreateSuccess("reCAPTCHA")
            : SecurityVerificationResult.CreateFailure("reCAPTCHA verification failed");
    }

    private async Task<SecurityVerificationResult> VerifyiOSSecurityAsync(SecurityVerificationRequest request)
    {
        // Priority 1: Apple DeviceCheck verification
        if (!string.IsNullOrEmpty(request.AcToken))
        {
            var deviceCheckValid = await VerifyAppleDeviceCheckAsync(request.AcToken);
            if (deviceCheckValid)
            {
                return SecurityVerificationResult.CreateSuccess("Apple DeviceCheck");
            }

            _logger.LogWarning("Apple DeviceCheck verification failed, falling back to reCAPTCHA");
        }

        // Fallback: reCAPTCHA verification
        if (!string.IsNullOrEmpty(request.RecaptchaToken))
        {
            var recaptchaValid = await VerifyRecaptchaAsync(request.RecaptchaToken, request.ClientIp);
            if (recaptchaValid)
            {
                return SecurityVerificationResult.CreateSuccess("reCAPTCHA (fallback for iOS)");
            }
        }

        return SecurityVerificationResult.CreateFailure(
            "iOS verification failed: both DeviceCheck and reCAPTCHA verification failed");
    }

    private async Task<SecurityVerificationResult> VerifyAndroidSecurityAsync(SecurityVerificationRequest request)
    {
        // Priority 1: Google Play Integrity verification
        if (!string.IsNullOrEmpty(request.AcToken))
        {
            var playIntegrityValid = await VerifyPlayIntegrityAsync(request.AcToken);
            if (playIntegrityValid)
            {
                return SecurityVerificationResult.CreateSuccess("Google Play Integrity");
            }

            _logger.LogWarning("Google Play Integrity verification failed, falling back to reCAPTCHA");
        }

        // Fallback: reCAPTCHA verification
        if (!string.IsNullOrEmpty(request.RecaptchaToken))
        {
            var recaptchaValid = await VerifyRecaptchaAsync(request.RecaptchaToken, request.ClientIp);
            if (recaptchaValid)
            {
                return SecurityVerificationResult.CreateSuccess("reCAPTCHA (fallback for Android)");
            }
        }

        return SecurityVerificationResult.CreateFailure(
            "Android verification failed: both Play Integrity and reCAPTCHA verification failed");
    }

    private async Task<bool> VerifyRecaptchaAsync(string token, string? remoteIp = null)
    {
        try
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("secret", _recaptchaOptions.SecretKey),
                new("response", token)
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                parameters.Add(new KeyValuePair<string, string>("remoteip", remoteIp));
            }

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(_recaptchaOptions.VerifyUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("reCAPTCHA API request failed: {statusCode}", response.StatusCode);
                return false;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("reCAPTCHA response: {response}", jsonResponse);

            var result = JsonSerializer.Deserialize<GoogleRecaptchaResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "reCAPTCHA verification exception");
            return false;
        }
    }

    private async Task<bool> VerifyAppleDeviceCheckAsync(string deviceToken)
    {
        try
        {
            if (string.IsNullOrEmpty(deviceToken))
            {
                _logger.LogWarning("Apple DeviceCheck token is empty");
                return false;
            }

            // Check if Apple DeviceCheck validation is enabled
            if (!_appleOptions.Enabled)
            {
                _logger.LogDebug("Apple DeviceCheck validation disabled, accepting token");
                return true;
            }

            // Basic token format validation
            if (!IsValidDeviceCheckToken(deviceToken))
            {
                _logger.LogWarning("Invalid Apple DeviceCheck token format");
                return false;
            }

            // Validate required configuration
            if (string.IsNullOrEmpty(_appleOptions.TeamId) ||
                string.IsNullOrEmpty(_appleOptions.KeyId) ||
                string.IsNullOrEmpty(_appleOptions.PrivateKey))
            {
                _logger.LogError("Apple DeviceCheck configuration incomplete: missing TeamId, KeyId, or PrivateKey");
                return false;
            }

            _logger.LogDebug("Apple DeviceCheck token received and validated: length={tokenLength}",
                deviceToken.Length);

            // Generate JWT for Apple DeviceCheck API authentication
            var jwt = GenerateAppleDeviceCheckJwt();
            if (string.IsNullOrEmpty(jwt))
            {
                _logger.LogError("Failed to generate JWT for Apple DeviceCheck API");
                return false;
            }

            // Call Apple DeviceCheck API to validate the device token
            var isValid = await CallAppleDeviceCheckApiAsync(deviceToken, jwt);
            _logger.LogInformation("Apple DeviceCheck validation result: {result}", isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple DeviceCheck verification exception");
            return false;
        }
    }

    private async Task<bool> VerifyPlayIntegrityAsync(string integrityToken)
    {
        try
        {
            if (string.IsNullOrEmpty(integrityToken))
            {
                _logger.LogWarning("Google Play Integrity token is empty");
                return false;
            }

            // Check if Play Integrity validation is enabled
            if (!_playOptions.Enabled)
            {
                _logger.LogDebug("Google Play Integrity validation disabled, accepting token");
                return true;
            }

            // Basic token format validation
            if (!IsValidPlayIntegrityToken(integrityToken))
            {
                _logger.LogWarning("Invalid Google Play Integrity token format");
                return false;
            }

            _logger.LogDebug("Google Play Integrity token received and validated: length={tokenLength}",
                integrityToken.Length);

            // TODO: Implement full Google Play Integrity API verification
            // This requires calling Google Play Integrity API with service account credentials
            // For now, accept valid format tokens
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Play Integrity verification exception");
            return false;
        }
    }

    private bool IsValidDeviceCheckToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            // DeviceCheck tokens are typically base64 encoded
            // Basic validation: length check and base64 format
            return token.Length > 50 &&
                   token.Length < 10000 &&
                   Convert.TryFromBase64String(token, new Span<byte>(new byte[token.Length * 3 / 4 + 3]), out _);
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPlayIntegrityToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            // Play Integrity tokens are JWT format
            // Basic validation: check if it looks like a JWT (3 parts separated by dots)
            var parts = token.Split('.');
            return parts.Length == 3 &&
                   parts.All(part => !string.IsNullOrWhiteSpace(part)) &&
                   token.Length > 100;
        }
        catch
        {
            return false;
        }
    }

    #region Apple DeviceCheck Implementation

    private string? GenerateAppleDeviceCheckJwt()
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var claims = new[]
            {
                new Claim("iss", _appleOptions.TeamId),
                new Claim("iat", now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer)
            };

            // Parse the private key
            var privateKey = ParseApplePrivateKey(_appleOptions.PrivateKey);
            if (privateKey == null)
            {
                _logger.LogError("Failed to parse Apple DeviceCheck private key");
                return null;
            }

            // Create ES256 signing credentials
            var signingCredentials = new SigningCredentials(
                new ECDsaSecurityKey(privateKey),
                SecurityAlgorithms.EcdsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory()
            };

            // Create JWT token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = now.AddMinutes(20).DateTime, // Apple recommends max 20 minutes
                SigningCredentials = signingCredentials
            };

            // Add kid (Key ID) to header
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
            token.Header["kid"] = _appleOptions.KeyId;
            token.Header["alg"] = "ES256";

            var jwt = tokenHandler.WriteToken(token);
            _logger.LogDebug("Generated Apple DeviceCheck JWT successfully");
            return jwt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Apple DeviceCheck JWT");
            return null;
        }
    }

    private ECDsa? ParseApplePrivateKey(string privateKeyPem)
    {
        try
        {
            // Remove header/footer and whitespace
            var privateKeyContent = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "");

            var privateKeyBytes = Convert.FromBase64String(privateKeyContent);
            var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            return ecdsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Apple private key");
            return null;
        }
    }

    private async Task<bool> CallAppleDeviceCheckApiAsync(string deviceToken, string jwt)
    {
        try
        {
            // Apple DeviceCheck API payload
            var payload = new
            {
                device_token = deviceToken,
                transaction_id = Guid.NewGuid().ToString(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");

            // Call Apple DeviceCheck API
            var response = await _httpClient.PostAsync(_appleOptions.ApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Apple DeviceCheck API request failed: {statusCode}, {content}",
                    response.StatusCode, errorContent);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Apple DeviceCheck API response: {response}", responseContent);

            // Parse response to check if device token is valid
            var responseObj = JsonSerializer.Deserialize<AppleDeviceCheckResponse>(responseContent);
            return responseObj != null; // If we get a valid response, consider it successful
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple DeviceCheck API call exception");
            return false;
        }
    }

    #endregion

    #region Helper Classes

    private class GoogleRecaptchaResponse
    {
        public bool Success { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string Hostname { get; set; } = "";
        [JsonPropertyName("error-codes")] public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    }

    private class AppleDeviceCheckResponse
    {
        [JsonPropertyName("bit0")] public bool? Bit0 { get; set; }

        [JsonPropertyName("bit1")] public bool? Bit1 { get; set; }

        [JsonPropertyName("last_update_time")] public string? LastUpdateTime { get; set; }
    }

    #endregion

    #endregion
}