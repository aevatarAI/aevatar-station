using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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


namespace Aevatar.Services;

/// <summary>
/// Unified security service implementation
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    private readonly RecaptchaOptions _recaptchaOptions;
    private readonly RateOptions _rateOptions;
    private readonly ILogger<SecurityService> _logger;
    
    // Static flag to ensure configuration is logged only once
    private static bool _hasLoggedConfiguration = false;

    public SecurityService(
        IDistributedCache cache,
        HttpClient httpClient,
        IOptions<RecaptchaOptions> recaptchaOptions,
        IOptions<RateOptions> rateOptions,
        ILogger<SecurityService> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _recaptchaOptions = recaptchaOptions.Value;
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
            
        // Log bypass platforms configuration for debugging  
        _logger.LogDebug("SecurityService BypassPlatforms Configuration: [{platforms}] (Count: {count})",
            string.Join(", ", _recaptchaOptions.BypassPlatforms), _recaptchaOptions.BypassPlatforms.Count);
            
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

    #region Complete Security Verification Flow

    public async Task<SecurityVerificationResult> PerformSecurityVerificationAsync(string clientIp, PlatformType platform, string? recaptchaToken, string operationName)
    {
        try
        {
            _logger.LogInformation("{operationName} security check started for Platform: {platform}", 
                operationName, platform);

            // Step 1: Increment request count immediately to prevent abuse
            var currentCount = await IncrementRequestCountAsync(clientIp);
            _logger.LogInformation("Request count incremented: {count}", currentCount);
            
            // Step 2: Check if security verification is required based on rate limiting and platform
            var verificationRequired = await IsSecurityVerificationRequiredAsync(clientIp, platform);
            
            if (!verificationRequired)
            {
                _logger.LogInformation("Platform {platform} - no security verification required", platform);
                return SecurityVerificationResult.CreateSuccess("No verification required");
            }

            _logger.LogInformation("Platform {platform} security verification required", platform);
            
            // Step 3: Perform security verification using reCAPTCHA
            var verificationRequest = new SecurityVerificationRequest
            {
                ClientIp = clientIp,
                RecaptchaToken = recaptchaToken
            };
            
            var verificationResult = await VerifySecurityAsync(verificationRequest);
            
            if (!verificationResult.Success)
            {
                _logger.LogWarning("Security verification failed: {reason}", 
                    verificationResult.Message);
                return verificationResult;
            }
            
            _logger.LogInformation("Security verification passed using {method}", 
                verificationResult.VerificationMethod);
            
            return verificationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during security verification for operation {operationName}", 
                operationName);
            return SecurityVerificationResult.CreateFailure("Security verification error occurred");
        }
    }

    #endregion

    #region Request Count and Rate Limiting

    public async Task<bool> IsSecurityVerificationRequiredAsync(string clientIp, PlatformType platform = PlatformType.Web)
    {
        if (!_rateOptions.Enabled)
        {
            return false;
        }

        // Check if current platform should be bypassed (if BypassPlatforms list contains it)
        if (_recaptchaOptions.BypassPlatforms.Count > 0 && 
            _recaptchaOptions.BypassPlatforms.Contains(platform.ToString()))
        {
            _logger.LogInformation("Platform {platform} bypassed security verification (platform in bypass list)",
                platform);
            return false;
        }

        var count = await GetTodayRequestCountAsync(clientIp);
        var required = count > _rateOptions.FreeRequestsPerDay;

        _logger.LogDebug("Platform {platform} daily request count: {count}, verification required: {required}",
            platform, count, required);

        return required;
    }

    public async Task<int> IncrementRequestCountAsync(string clientIp)
    {
        var key = GetCacheKey(clientIp);
        var expiry = GetExpiryTime();
        
        // Try to increment atomically, if key doesn't exist, create it with value 1
        var newCount = await IncrementAtomicallyAsync(key, expiry);

        _logger.LogDebug("Daily request count incremented to: {count}", newCount);
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

    private async Task<int> GetTodayRequestCountAsync(string clientIp)
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
        // Generate cache key based on daily windows
        var now = DateTime.UtcNow;
        var dateKey = now.ToString("yyyyMMdd");
        var cacheKey = $"SendRegCode:{clientIp}:{dateKey}";
        
        return cacheKey;
    }

    private DateTimeOffset GetExpiryTime()
    {
        // Set expiry to end of current day (24-hour window)
        var now = DateTime.UtcNow;
        var dayEnd = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59).AddSeconds(1);
        return new DateTimeOffset(dayEnd);
    }

    #endregion

    #region Security Verification

    public async Task<SecurityVerificationResult> VerifySecurityAsync(SecurityVerificationRequest request)
    {
        _logger.LogInformation("Security verification: EnableRecaptcha={enabled}, HasToken={hasToken}", 
            _recaptchaOptions.Enabled, !string.IsNullOrEmpty(request.RecaptchaToken));
            
        if (!_recaptchaOptions.Enabled)
        {
            _logger.LogWarning("reCAPTCHA verification disabled, skipping verification - this allows bypass!");
            return SecurityVerificationResult.CreateSuccess("reCAPTCHA (disabled)");
        }

        if (string.IsNullOrEmpty(request.RecaptchaToken))
        {
            _logger.LogWarning("Missing reCAPTCHA token for verification");
            return SecurityVerificationResult.CreateFailure("Missing reCAPTCHA verification token");
        }

        var isValid = await VerifyRecaptchaAsync(request.RecaptchaToken, request.ClientIp);
        return isValid
            ? SecurityVerificationResult.CreateSuccess("reCAPTCHA")
            : SecurityVerificationResult.CreateFailure("reCAPTCHA verification failed");
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















    #region Helper Classes

    private class GoogleRecaptchaResponse
    {
        public bool Success { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string Hostname { get; set; } = "";
        [JsonPropertyName("error-codes")] public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    }



    #endregion

    #endregion
}