using System.Linq;
using Microsoft.AspNetCore.Http;
using Aevatar.Domain.Shared;

namespace Aevatar.Extensions;

/// <summary>
/// HttpContext extension methods
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Get GodgptLanguage from request headers
    /// </summary>
    /// <param name="context">The HttpContext instance</param>
    /// <returns>GodgptLanguage enum value, defaults to English if not found or invalid</returns>
    public static GodGPTChatLanguage GetGodGPTLanguage(this HttpContext context)
    {
        var languageHeader = context.Request.Headers["GodgptLanguage"].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(languageHeader))
        {
            return GodGPTChatLanguage.English; // Default to English
        }
        
        return languageHeader.ToLowerInvariant() switch
        {
            "en" => GodGPTChatLanguage.English,
            "zh-cn" => GodGPTChatLanguage.CN,
            "zh-tw" => GodGPTChatLanguage.TraditionalChinese,
            "es" => GodGPTChatLanguage.Spanish,
            "zh" => GodGPTChatLanguage.CN,
            _ => GodGPTChatLanguage.English // Default to English for unknown values
        };
    }

    /// <summary>
    /// Append language-specific prompt requirement to the message
    /// </summary>
    /// <param name="message">Original message</param>
    /// <param name="language">Target language for response</param>
    /// <returns>Message with language requirement appended</returns>
    public static string AppendLanguagePrompt(this string message, GodGPTChatLanguage language)
    {
        var promptMsg = message;

        /*promptMsg += language switch
        {
            GodGPTLanguage.English => ".Requirement: Please reply in English.",
            GodGPTLanguage.TraditionalChinese => ".Requirement: Please reply in Chinese.",
            GodGPTLanguage.Spanish => ".Requirement: Please reply in Spanish.",
            _ => ".Requirement: Please reply in English."
        };*/

        return promptMsg;
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