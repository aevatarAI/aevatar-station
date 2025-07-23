using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Aevatar.Extensions;

/// <summary>
/// GodgptLanguage enumeration for supported languages
/// </summary>
public enum GodgptLanguage
{
    /// <summary>
    /// English language
    /// </summary>
    En = 0,
    
    /// <summary>
    /// Traditional Chinese language
    /// </summary>
    ZhTw = 1,
    
    /// <summary>
    /// Spanish language
    /// </summary>
    Es = 2
}

/// <summary>
/// HttpContext extension methods
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Get GodgptLanguage from request headers
    /// </summary>
    /// <param name="context">The HttpContext instance</param>
    /// <returns>GodgptLanguage enum value, defaults to En if not found or invalid</returns>
    public static GodgptLanguage GetGodgptLanguage(this HttpContext context)
    {
        var languageHeader = context.Request.Headers["GodgptLanguage"].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(languageHeader))
        {
            return GodgptLanguage.En; // Default to English
        }
        
        return languageHeader.ToLowerInvariant() switch
        {
            "en" => GodgptLanguage.En,
            "zh-tw" => GodgptLanguage.ZhTw,
            "es" => GodgptLanguage.Es,
            _ => GodgptLanguage.En // Default to English for unknown values
        };
    }

    /// <summary>
    /// Get client IP address from request headers with proxy support
    /// </summary>
    /// <param name="context">The HttpContext instance</param>
    /// <returns>Client IP address as string</returns>
    public static string GetClientIpAddress(this HttpContext context)
    {
        // Check for X-Forwarded-For header (common with load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for X-Real-IP header (used by nginx and other proxies)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to HttpContext.Connection.RemoteIpAddress
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(remoteIp))
        {
            return remoteIp;
        }

        // Last resort fallback
        return "127.0.0.1";
    }
} 