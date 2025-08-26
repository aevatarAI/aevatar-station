using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Emailing;
using Volo.Abp.Caching;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
using Aevatar.Domain.Shared;
using Exception = System.Exception;
using Newtonsoft.Json;

namespace Aevatar.Account;

[RemoteService(IsEnabled = false)]
public class AccountService : AccountAppService, IAccountService
{
    private readonly IAevatarAccountEmailer _aevatarAccountEmailer;
    private readonly AccountOptions _accountOptions;
    private readonly IDistributedCache<string,string> _registerCode;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions;
    private readonly ILogger<AccountService> _logger;
    private readonly ILocalizationService _localizationService;
    
    public AccountService(IdentityUserManager userManager, IIdentityRoleRepository roleRepository,
        IAccountEmailer accountEmailer, IdentitySecurityLogManager identitySecurityLogManager,
        IOptions<IdentityOptions> identityOptions, IAevatarAccountEmailer aevatarAccountEmailer,
        IOptionsSnapshot<AccountOptions> accountOptions, IDistributedCache<string, string> registerCode,
        ILogger<AccountService> logger, ILocalizationService localizationService)
        : base(userManager, roleRepository, accountEmailer, identitySecurityLogManager, identityOptions)
    {
        _aevatarAccountEmailer = aevatarAccountEmailer;
        _registerCode = registerCode;
        _accountOptions = accountOptions.Value;
        _logger = logger;
        _localizationService = localizationService;

        _defaultCacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_accountOptions.RegisterCodeDuration)
        };
    }

    public async Task<IdentityUserDto> RegisterAsync(AevatarRegisterDto input, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        var code = await _registerCode.GetAsync(GetRegisterCodeKey(input.EmailAddress));
        if (code != input.Code)
        {
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidCaptchaCode, language);
            throw new UserFriendlyException(localizedMessage);
        }

        await IdentityOptions.SetAsync();

        var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.EmailAddress);

        input.MapExtraPropertiesTo(user);

        (await UserManager.CreateAsync(user, input.Password)).CheckErrors();

        await UserManager.SetEmailAsync(user, input.EmailAddress);
        await UserManager.AddDefaultRolesAsync(user);

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(user);
    }

    public async Task SendRegisterCodeAsync(SendRegisterCodeDto input, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        var user = await UserManager.FindByEmailAsync(input.Email);
        if (user != null)
        {
            var parameters = new Dictionary<string, string>
            {
                ["input.Email"] = input.Email
            };
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.HASREGISTERED, language, parameters);
            throw new UserFriendlyException(localizedMessage);
        }

        var code = GenerateVerificationCode();
        await _registerCode.SetAsync(GetRegisterCodeKey(input.Email), code, _defaultCacheOptions);
        await _aevatarAccountEmailer.SendRegisterCodeAsync(input.Email, code, language);
    }

    public async Task<bool> VerifyRegisterCodeAsync(VerifyRegisterCodeDto input)
    {
        var code = await _registerCode.GetAsync(GetRegisterCodeKey(input.Email));
        return code == input.Code;
    }

    public override async Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input)
    {
        var user = await GetUserByEmailAsync(input.Email);
        if (user == null)
        {
            _logger.LogWarning("[AccountService][SendPasswordResetCodeAsync] {Email} User not found.", input.Email);
            return;
        }
        var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user);
        await _aevatarAccountEmailer.SendPasswordResetLinkAsync(user, input.Email, resetToken);
    }
    
    public async Task<bool> CheckEmailRegisteredAsync(CheckEmailRegisteredDto input)
    {
        var existingUser = await UserManager.FindByEmailAsync(input.EmailAddress);
        return existingUser != null;
    }
    
    public async Task<bool> VerifyEmailRegistrationWithTimeAsync(CheckEmailRegisteredDto input)
    {
        var existingUser = await UserManager.FindByEmailAsync(input.EmailAddress);
        if (existingUser == null)
        {
            _logger.LogDebug("[AccountService][CheckEmailRegisteredAsync] Email not registered: {0}", input.EmailAddress);
            return false;
        }
        
        // Check if the user was registered within the last 24 hours
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
        _logger.LogDebug(
            "[AccountService][CheckEmailRegisteredAsync] User found. Email: {0}, CreationTime: {1}, ThresholdTime: {2}", 
            input.EmailAddress, 
            existingUser.CreationTime, 
            twentyFourHoursAgo
        );
        return existingUser.CreationTime >= twentyFourHoursAgo;
    }
    
    public async Task<IdentityUserDto> GodgptRegisterAsync(GodGptRegisterDto input, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        var code = await _registerCode.GetAsync(GetRegisterCodeKey(input.EmailAddress));
        if (code != input.Code)
        {
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidCaptchaCode, language);
            throw new UserFriendlyException(localizedMessage);
        }

        await IdentityOptions.SetAsync();
        var userName = input.UserName.IsNullOrWhiteSpace() ? GuidGenerator.Create().ToString() : input.UserName;
        var user = new IdentityUser(GuidGenerator.Create(), userName, input.EmailAddress);
    
        input.MapExtraPropertiesTo(user);

        try
        {
            (await UserManager.CreateAsync(user, input.Password)).CheckErrors();
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message.ToLower();
            if (errorMessage.Contains("username") && errorMessage.Contains("is invalid"))
            {
                var localizedMessage =
                    _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.InvalidUserName, language);
                throw new Exception(localizedMessage);
            }
            _logger.LogError(
                    $"[GodgptRegisterAsync] error UserFriendlyException. Email: {input.EmailAddress} input:{JsonConvert.SerializeObject(input)} error:{ex.Message}");
            throw ex;
        }

        await UserManager.SetEmailAsync(user, input.EmailAddress);
        await UserManager.AddDefaultRolesAsync(user);

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(user);
    }
    
    private string GenerateVerificationCode()
    {
        var random = new Random();
        var code = random.Next(0, 999999);
        return code.ToString("D6");
    }

    private string GetRegisterCodeKey(string email)
    {
        return $"RegisterCode_{email.ToLower()}";
    }
}