using System;
using System.Net;
using System.Threading.Tasks;
using Aevatar.Account;
using Asp.Versioning;
using Volo.Abp;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Aevatar.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Account")]
[Route("api/account")]
public class AccountController : AevatarController
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
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
    
    [HttpPost]
    [Route("send-register-code")]
    public virtual Task SendRegisterCodeAsync(SendRegisterCodeDto input)
    {
        var ip = GetRealClientIp(HttpContext);
        var language = HttpContext.GetGodGPTLanguage();
        Logger.LogDebug("Send register code request: Email={email}, AppName={appName}, IP={ip}", 
            input.Email, input.AppName, ip);
        return _accountService.SendRegisterCodeAsync(input, language);
    }
    
    private string GetRealClientIp(HttpContext context)
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
                return ip;
            }
        }

        // 3. Use connection remote IP
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return remoteIp;
    }
    
    private bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
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
        var language = HttpContext.GetGodGPTLanguage();
        return _accountService.SendPasswordResetCodeAsync(input, language);
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