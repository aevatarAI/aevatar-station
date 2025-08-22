using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Aevatar.Common;

namespace Aevatar.Services;

/// <summary>
/// Security service interface for authentication and verification
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Get the real client IP address from HTTP context
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client IP address</returns>
    string GetRealClientIp(HttpContext context);

    /// <summary>
    /// Check if security verification is required for the client based on platform
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    /// <param name="platform">Platform type (Web, iOS, Android)</param>
    /// <returns>True if verification is required</returns>
    Task<bool> IsSecurityVerificationRequiredAsync(string clientIp, PlatformType platform = PlatformType.Web);

    /// <summary>
    /// Verify security based on platform and provided tokens
    /// </summary>
    /// <param name="request">Security verification request</param>
    /// <returns>Verification result</returns>
    Task<SecurityVerificationResult> VerifySecurityAsync(SecurityVerificationRequest request);

    /// <summary>
    /// Increment request count for rate limiting (atomic operation)
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    /// <returns>New count after increment</returns>
    Task<int> IncrementRequestCountAsync(string clientIp);
}

/// <summary>
/// Security verification request model - unified reCAPTCHA verification
/// </summary>
public class SecurityVerificationRequest
{
    public string ClientIp { get; set; } = "";
    public string? RecaptchaToken { get; set; }
}

/// <summary>
/// Security verification result model
/// </summary>
public class SecurityVerificationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string VerificationMethod { get; set; } = "";

    public static SecurityVerificationResult CreateSuccess(string method) =>
        new() { Success = true, Message = "Verification successful", VerificationMethod = method };

    public static SecurityVerificationResult CreateFailure(string message) =>
        new() { Success = false, Message = message };
}