using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Account;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
using Aevatar.Application.Service;
using Aevatar.Domain.Shared;
using Aevatar.Extensions;
using Aevatar.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Threading;

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
    private readonly IIpLocationService _ipLocationService;

    public AccountController(
        IAccountService accountService,
        ISecurityService securityService,
        ILocalizationService localizationService,
        ILogger<AccountController> logger,
        IIpLocationService ipLocationService)
    {
        _accountService = accountService;
        _securityService = securityService;
        _localizationService = localizationService;
        _logger = logger;
        _ipLocationService = ipLocationService;
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

        try
        {
            // Perform security verification (rate limiting + platform-based verification)
            await _securityService.ValidateSecurityAsync(
                HttpContext, 
                input.Platform, 
                input.RecaptchaToken, 
                nameof(SendRegisterCodeAsync),
                _localizationService,
                language,
                _logger);
            
            // Call the account service to send verification code
            await _accountService.SendRegisterCodeAsync(input, language);
            
            _logger.LogInformation("Verification code sent successfully for email {email}", input.Email);
            
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
            _logger.LogError(ex, "Error sending registration code for email {email}", 
                input.Email);
            
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
    public virtual async Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input)
    {
        var clientIp = HttpContext.GetClientIpAddress();
        var isCN = await _ipLocationService.IsInMainlandChinaAsync(clientIp);
        RequestContext.Set("IsCN", isCN);
        var language = HttpContext.GetGodGPTLanguage();
        await _accountService.SendPasswordResetCodeAsync(input, language);
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