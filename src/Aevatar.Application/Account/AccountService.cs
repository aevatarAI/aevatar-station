using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Emailing;
using Volo.Abp.Caching;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Aevatar.Account;

[RemoteService(IsEnabled = false)]
public class AccountService : AccountAppService, IAccountService
{
    private readonly IAevatarAccountEmailer _aevatarAccountEmailer;
    private readonly AccountOptions _accountOptions;
    private readonly IDistributedCache<string,string> _registerCode;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions;

    public AccountService(IdentityUserManager userManager, IIdentityRoleRepository roleRepository,
        IAccountEmailer accountEmailer, IdentitySecurityLogManager identitySecurityLogManager,
        IOptions<IdentityOptions> identityOptions, IAevatarAccountEmailer aevatarAccountEmailer,
        IOptionsSnapshot<AccountOptions> accountOptions, IDistributedCache<string, string> registerCode)
        : base(userManager, roleRepository, accountEmailer, identitySecurityLogManager, identityOptions)
    {
        _aevatarAccountEmailer = aevatarAccountEmailer;
        _registerCode = registerCode;
        _accountOptions = accountOptions.Value;

        _defaultCacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_accountOptions.RegisterCodeDuration)
        };
    }

    public async Task<IdentityUserDto> RegisterAsync(AevatarRegisterDto input)
    {
        var code = await _registerCode.GetAsync(GetRegisterCodeKey(input.EmailAddress));
        if (code != input.Code)
        {
            throw new UserFriendlyException("Invalid captcha code");
        }

        await IdentityOptions.SetAsync();

        var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.EmailAddress);

        input.MapExtraPropertiesTo(user);

        (await UserManager.CreateAsync(user, input.Password)).CheckErrors();

        await UserManager.SetEmailAsync(user, input.EmailAddress);
        await UserManager.AddDefaultRolesAsync(user);

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(user);
    }

    public async Task<IdentityUserDto> GodgptRegisterAsync(GodGptRegisterDto input)
    {
        var code = await _registerCode.GetAsync(GetRegisterCodeKey(input.EmailAddress));
        if (code != input.Code)
        {
            throw new UserFriendlyException("Invalid captcha code");
        }

        await IdentityOptions.SetAsync();
        var userName = input.UserName.IsNullOrWhiteSpace() ? GuidGenerator.Create().ToString() : input.UserName;
        var user = new IdentityUser(GuidGenerator.Create(), userName, input.EmailAddress);
    
        input.MapExtraPropertiesTo(user);

        (await UserManager.CreateAsync(user, input.Password)).CheckErrors();

        await UserManager.SetEmailAsync(user, input.EmailAddress);
        await UserManager.AddDefaultRolesAsync(user);

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(user);
    }

    public async Task SendRegisterCodeAsync(SendRegisterCodeDto input)
    {
        var user = await UserManager.FindByEmailAsync(input.Email);
        if (user != null)
        {
            throw new UserFriendlyException($"The email: {input.Email} has been registered.");
        }
        
        var code = GenerateVerificationCode();
        await _registerCode.SetAsync(GetRegisterCodeKey(input.Email), code, _defaultCacheOptions);
        await _aevatarAccountEmailer.SendRegisterCodeAsync(input.Email, code);
    }

    public override async Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input)
    {
        var user = await GetUserByEmailAsync(input.Email);
        var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user);
        await _aevatarAccountEmailer.SendPasswordResetLinkAsync(user, resetToken);
    }

    public async Task<bool> CheckEmailRegisteredAsync(CheckEmailRegisteredDto input)
    {
        var existingUser = await UserManager.FindByEmailAsync(input.EmailAddress);
        return existingUser != null;
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