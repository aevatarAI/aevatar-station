using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.Common;
using Aevatar.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Aevatar.Services;

/// <summary>
/// Unified security service implementation
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    private readonly SecurityOptions _options;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        IDistributedCache cache,
        HttpClient httpClient,
        IOptions<SecurityOptions> options,
        ILogger<SecurityService> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.ReCAPTCHA.TimeoutSeconds);
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
        if (_options.Switch?.EnableRateLimit != true)
        {
            return false;
        }

        var count = await GetTodayRequestCountAsync(clientIp);
        var required = count >= _options.RateLimit.FreeRequestsPerDay;
        
        _logger.LogDebug("IP {ip} today request count: {count}, verification required: {required}", 
            clientIp, count, required);
        
        return required;
    }

    public async Task IncrementRequestCountAsync(string clientIp)
    {
        var key = GetCacheKey(clientIp);
        var currentCount = await GetTodayRequestCountAsync(clientIp);
        var newCount = currentCount + 1;
        
        var expiry = GetExpiryTime();
        await _cache.SetStringAsync(key, newCount.ToString(), 
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiry
            });
        
        _logger.LogDebug("IP {ip} request count updated: {count}", clientIp, newCount);
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
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        return $"{_options.RateLimit.CacheKeyPrefix}{clientIp}:{today}";
    }

    private DateTimeOffset GetExpiryTime()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        return new DateTimeOffset(tomorrow);
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
        if (_options.Switch?.EnableReCAPTCHA != true)
        {
            _logger.LogDebug("Web reCAPTCHA verification disabled, skipping verification");
            return SecurityVerificationResult.CreateSuccess("reCAPTCHA (disabled)");
        }

        if (string.IsNullOrEmpty(request.ReCAPTCHAToken))
        {
            return SecurityVerificationResult.CreateFailure("Missing reCAPTCHA verification token");
        }

        var isValid = await VerifyReCAPTCHAAsync(request.ReCAPTCHAToken, request.ClientIp);
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
        if (!string.IsNullOrEmpty(request.ReCAPTCHAToken))
        {
            var recaptchaValid = await VerifyReCAPTCHAAsync(request.ReCAPTCHAToken, request.ClientIp);
            if (recaptchaValid)
            {
                return SecurityVerificationResult.CreateSuccess("reCAPTCHA (fallback for iOS)");
            }
        }

        return SecurityVerificationResult.CreateFailure("iOS verification failed: both DeviceCheck and reCAPTCHA verification failed");
    }

    private async Task<SecurityVerificationResult> VerifyAndroidSecurityAsync(SecurityVerificationRequest request)
    {
        // Android currently only supports reCAPTCHA
        if (!string.IsNullOrEmpty(request.ReCAPTCHAToken))
        {
            var recaptchaValid = await VerifyReCAPTCHAAsync(request.ReCAPTCHAToken, request.ClientIp);
            if (recaptchaValid)
            {
                return SecurityVerificationResult.CreateSuccess("reCAPTCHA (Android)");
            }
        }

        return SecurityVerificationResult.CreateFailure("Android verification failed: reCAPTCHA verification required");
    }

    private async Task<bool> VerifyReCAPTCHAAsync(string token, string? remoteIp = null)
    {
        try
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("secret", _options.ReCAPTCHA.SecretKey),
                new("response", token)
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                parameters.Add(new KeyValuePair<string, string>("remoteip", remoteIp));
            }

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(_options.ReCAPTCHA.VerifyUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("reCAPTCHA API request failed: {statusCode}", response.StatusCode);
                return false;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("reCAPTCHA response: {response}", jsonResponse);

            var result = JsonSerializer.Deserialize<GoogleReCAPTCHAResponse>(jsonResponse, new JsonSerializerOptions
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
            if (_options.AppleDeviceCheck?.EnableValidation != true)
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

            _logger.LogDebug("Apple DeviceCheck token received and validated: length={tokenLength}", deviceToken.Length);

            // TODO: Implement full Apple DeviceCheck API verification
            // This requires calling Apple's DeviceCheck API with proper JWT authentication
            // For now, accept valid format tokens
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple DeviceCheck verification exception");
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

    #endregion

    #region Helper Classes
    
    private class GoogleReCAPTCHAResponse
    {
        public bool Success { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string Hostname { get; set; } = "";
        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    }

    #endregion
}
