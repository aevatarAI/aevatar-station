using System;
using System.Threading.Tasks;
using Aevatar.Account;
using Aevatar.Services;
using Asp.Versioning;
using Volo.Abp;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Aevatar.Extensions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Account")]
[Route("api/account")]
public class AccountController : AevatarController
{
    private readonly IAccountService _accountService;
    private readonly ISecurityService _securityService;

    public AccountController(
        IAccountService accountService,
        ISecurityService securityService)
    {
        _accountService = accountService;
        _securityService = securityService;
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
    /// Send registration verification code with security validation
    /// </summary>
    /// <param name="input">Request parameters</param>
    /// <returns>Operation result</returns>
    [HttpPost]
    [Route("send-register-code")]
    public virtual async Task<IActionResult> SendRegisterCodeAsync(SendRegisterCodeDto input)
    {
        try
        {
            // Perform security validation
            var securityValidationResult = await ValidateSecurityAsync(input);
            if (!securityValidationResult.Success)
            {
                return BadRequest(securityValidationResult.ErrorResponse);
            }

            // Send verification code
            var language = HttpContext.GetGodGPTLanguage();
            await _accountService.SendRegisterCodeAsync(input, language);

            Logger.LogInformation("Register code sent successfully: Email={email}, Platform={platform}, IP={ip}", 
                input.Email, input.Platform, securityValidationResult.ClientIp);

            return Ok(new SendRegisterCodeResponseDto
            {
                Success = true,
                Message = "Verification code sent successfully"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send register code exception: Email={email}, Platform={platform}", 
                input.Email, input.Platform);
            
            return StatusCode(500, new SendRegisterCodeResponseDto
            {
                Success = false,
                Message = "Internal server error, please try again later"
            });
        }
    }

    /// <summary>
    /// Validate security requirements for send register code request
    /// </summary>
    /// <param name="input">Request parameters</param>
    /// <returns>Security validation result</returns>
    private async Task<SecurityValidationResult> ValidateSecurityAsync(SendRegisterCodeDto input)
    {
        // 1. Get real client IP address
        var clientIp = _securityService.GetRealClientIp(HttpContext);
        Logger.LogInformation("Send register code request: Email={email}, Platform={platform}, IP={ip}", 
            input.Email, input.Platform, clientIp);

        // 2. Check if security verification is required based on current count
        var needsVerification = await _securityService.IsSecurityVerificationRequiredAsync(clientIp);

        // 3. Always increment request count to prevent abuse (regardless of verification result)
        await _securityService.IncrementRequestCountAsync(clientIp);

        if (needsVerification)
        {
            // 4. Perform security verification
            var verificationRequest = new SecurityVerificationRequest
            {
                Platform = input.Platform,
                ClientIp = clientIp,
                ReCAPTCHAToken = input.ReCAPTCHAToken,
                AcToken = input.AcToken
            };

            var verificationResult = await _securityService.VerifySecurityAsync(verificationRequest);
            if (!verificationResult.Success)
            {
                Logger.LogWarning("Security verification failed: Email={email}, Platform={platform}, IP={ip}, Reason={reason}", 
                    input.Email, input.Platform, clientIp, verificationResult.Message);
                
                return SecurityValidationResult.CreateFailure(clientIp, new SendRegisterCodeResponseDto
                {
                    Success = false,
                    Message = verificationResult.Message
                });
            }

            Logger.LogInformation("Security verification successful: Email={email}, Platform={platform}, IP={ip}, Method={method}", 
                input.Email, input.Platform, clientIp, verificationResult.VerificationMethod);
        }

        return SecurityValidationResult.CreateSuccess(clientIp);
    }

    /// <summary>
    /// Security validation result for internal use
    /// </summary>
    private class SecurityValidationResult
    {
        public bool Success { get; set; }
        public string ClientIp { get; set; } = "";
        public SendRegisterCodeResponseDto? ErrorResponse { get; set; }

        public static SecurityValidationResult CreateSuccess(string clientIp) => 
            new() { Success = true, ClientIp = clientIp };

        public static SecurityValidationResult CreateFailure(string clientIp, SendRegisterCodeResponseDto errorResponse) => 
            new() { Success = false, ClientIp = clientIp, ErrorResponse = errorResponse };
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