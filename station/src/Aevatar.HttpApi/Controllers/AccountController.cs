using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Account;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
using Aevatar.Domain.Shared;
using Aevatar.Extensions;
using Aevatar.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Identity;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Account")]
[Route("api/account")]
public class AccountController : AevatarController
{
    private readonly IAccountService _accountService;
    private readonly ISecurityService _securityService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        ISecurityService securityService,
        ILocalizationService localizationService,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _securityService = securityService;
        _localizationService = localizationService;
        _logger = logger;
    }
    
    [HttpPost]
    [Route("register")]
    public virtual Task<IdentityUserDto> RegisterAsync(AevatarRegisterDto input)
    {
        var language = HttpContext.GetGodGPTLanguage();
        return _accountService.RegisterAsync(input, language);
    }
    
    [HttpPost]
    [Route("godgpt-register")]
    public virtual Task<IdentityUserDto> GodgptRegisterAsync(GodGptRegisterDto input)
    {
        var language = HttpContext.GetGodGPTLanguage();
        return _accountService.GodgptRegisterAsync(input, language);
    }
    
    /// <summary>
    /// Send registration verification code with security verification
    /// </summary>
    /// <param name="input">Request parameters including platform type and security tokens</param>
    /// <returns>Operation result</returns>
    [HttpPost]
    [Route("send-register-code")]
    public virtual async Task<SendRegisterCodeResponseDto> SendRegisterCodeAsync(SendRegisterCodeDto input)
    {
        var language = HttpContext.GetGodGPTLanguage();
        var clientIp = _securityService.GetRealClientIp(HttpContext);
        
        _logger.LogInformation("SendRegisterCodeAsync request from IP {clientIp}, Email: {email}, Platform: {platform}", 
            clientIp, input.Email, input.Platform);

        try
        {
            // Step 1: Increment request count immediately to prevent abuse (Option A - Anti-spam priority)
            var currentCount = await _securityService.IncrementRequestCountAsync(clientIp);
            _logger.LogInformation("IP {clientIp} request count incremented: {count}", clientIp, currentCount);
            
            // Step 2: Check if security verification is required based on rate limiting and platform
            var verificationRequired = await _securityService.IsSecurityVerificationRequiredAsync(clientIp, input.Platform);
            
            if (verificationRequired)
            {
                _logger.LogInformation("IP {clientIp} platform {platform} security verification required", clientIp, input.Platform);
                
                // Step 3: Perform security verification using reCAPTCHA
                var verificationRequest = new SecurityVerificationRequest
                {
                    ClientIp = clientIp,
                    RecaptchaToken = input.RecaptchaToken
                };
                
                var verificationResult = await _securityService.VerifySecurityAsync(verificationRequest);
                
                if (!verificationResult.Success)
                {
                    _logger.LogWarning("IP {clientIp} security verification failed: {reason}", 
                        clientIp, verificationResult.Message);
                    
                    // Return SecurityVerificationRequired (always English) as frontend signal
                    if (verificationResult.Message.Contains("Missing") || verificationResult.Message.Contains("token"))
                    {
                        // This tells frontend that verification is needed
                        throw new UserFriendlyException(
                            _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.SecurityVerificationRequired, 
                                GodGPTChatLanguage.English, // Always use English for frontend detection
                                new Dictionary<string, string>()));
                    }
                    else
                    {
                        // This tells user about verification failure with localized message
                        var parameters = new Dictionary<string, string> { ["reason"] = verificationResult.Message };
                        throw new UserFriendlyException(
                            _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.SecurityVerificationFailed, 
                                language, parameters));
                    }
                }
                
                _logger.LogInformation("IP {clientIp} security verification passed using {method}", 
                    clientIp, verificationResult.VerificationMethod);
            }
            
            // Step 4: Call the account service to send verification code
            await _accountService.SendRegisterCodeAsync(input, language);
            
            _logger.LogInformation("Verification code sent successfully for email {email} from IP {clientIp}", 
                input.Email, clientIp);
            
            return new SendRegisterCodeResponseDto
            {
                Success = true,
                Message = "Verification code sent successfully"
            };
        }
        catch (UserFriendlyException)
        {
            // Re-throw UserFriendlyException as-is (already localized)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending registration code for email {email} from IP {clientIp}", 
                input.Email, clientIp);
            
            // Return localized internal server error
            throw new UserFriendlyException(
                _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InternalServerError, 
                    language, new Dictionary<string, string>()));
        }
    }

    [HttpPost]
    [Route("verify-register-code")]
    public virtual Task<bool> VerifyRegisterCodeAsync(VerifyRegisterCodeDto input)
    {
        return _accountService.VerifyRegisterCodeAsync(input);
    }

    [HttpPost]
    [Route("send-password-reset-code")]
    public virtual Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input)
    {
        return _accountService.SendPasswordResetCodeAsync(input);
    }

    [HttpPost]
    [Route("verify-password-reset-token")]
    public virtual Task<bool> VerifyPasswordResetTokenAsync(VerifyPasswordResetTokenInput input)
    {
        return _accountService.VerifyPasswordResetTokenAsync(input);
    }

    [HttpPost]
    [Route("reset-password")]
    public virtual Task ResetPasswordAsync(ResetPasswordDto input)
    {
        return _accountService.ResetPasswordAsync(input);
    }
    
    [HttpPost]
    [Route("check-email-registered")]
    public virtual Task<bool> CheckEmailRegisteredAsync(CheckEmailRegisteredDto input)
    {
        return _accountService.CheckEmailRegisteredAsync(input);
    }
}