using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
using Aevatar.Common;
using Aevatar.Domain.Shared;
using Aevatar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Extensions;

/// <summary>
/// Security verification helper extensions for controllers
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Perform security verification and throw appropriate exceptions if failed
    /// </summary>
    /// <param name="securityService">Security service instance</param>
    /// <param name="httpContext">HTTP context</param>
    /// <param name="platform">Platform type</param>
    /// <param name="recaptchaToken">reCAPTCHA token</param>
    /// <param name="operationName">Operation name for logging</param>
    /// <param name="localizationService">Localization service</param>
    /// <param name="language">User language</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Task representing the verification operation</returns>
    /// <exception cref="UserFriendlyException">Thrown when verification fails</exception>
    public static async Task ValidateSecurityAsync(
        this ISecurityService securityService,
        HttpContext httpContext,
        PlatformType platform,
        string? recaptchaToken,
        string operationName,
        ILocalizationService localizationService,
        GodGPTChatLanguage language,
        ILogger logger)
    {
        var clientIp = securityService.GetRealClientIp(httpContext);
        
        logger.LogInformation("{operationName} request from IP {clientIp}, Platform: {platform}", 
            operationName, clientIp, platform);

        var verificationResult = await securityService.PerformSecurityVerificationAsync(
            clientIp, platform, recaptchaToken, operationName);

        if (!verificationResult.Success)
        {
            logger.LogWarning("IP {clientIp} {operationName} security verification failed: {reason}", 
                clientIp, operationName, verificationResult.Message);
            
            // Return SecurityVerificationRequired (always English) as frontend signal
            if (verificationResult.Message.Contains("Missing") || verificationResult.Message.Contains("token"))
            {
                // This tells frontend that verification is needed
                throw new UserFriendlyException(
                    localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.SecurityVerificationRequired, 
                        GodGPTChatLanguage.English, // Always use English for frontend detection
                        new Dictionary<string, string>()));
            }
            else
            {
                // This tells user about verification failure with localized message
                var parameters = new Dictionary<string, string> { ["reason"] = verificationResult.Message };
                throw new UserFriendlyException(
                    localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.SecurityVerificationFailed, 
                        language, parameters));
            }
        }
        
        logger.LogInformation("IP {clientIp} {operationName} security verification passed using {method}", 
            clientIp, operationName, verificationResult.VerificationMethod);
    }
}
