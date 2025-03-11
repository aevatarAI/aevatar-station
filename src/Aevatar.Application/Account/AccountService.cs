using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Aevatar.Account;

[RemoteService(IsEnabled = false)]
public class AccountService : AccountAppService, IAccountService
{
    private readonly IAevatarAccountEmailer _aevatarAccountEmailer;

    public AccountService(IdentityUserManager userManager, IIdentityRoleRepository roleRepository,
        IAccountEmailer accountEmailer, IdentitySecurityLogManager identitySecurityLogManager,
        IOptions<IdentityOptions> identityOptions, IAevatarAccountEmailer aevatarAccountEmailer) : base(userManager,
        roleRepository, accountEmailer,
        identitySecurityLogManager, identityOptions)
    {
        _aevatarAccountEmailer = aevatarAccountEmailer;
    }

    public async Task<IdentityUserDto> RegisterAsync(AevatarRegisterDto input)
    {
        await IdentityOptions.SetAsync();

        var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.EmailAddress);

        input.MapExtraPropertiesTo(user);

        (await UserManager.CreateAsync(user, input.Password)).CheckErrors();

        await UserManager.SetEmailAsync(user, input.EmailAddress);
        await UserManager.AddDefaultRolesAsync(user);

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(user);
    }

    public async Task SendRegisterCodeAsync(SendRegisterCodeDto input)
    {
        var code = GenerateVerificationCode();
        await _aevatarAccountEmailer.SendRegisterCodeAsync(input.Email, code);
    }

    public override async Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input)
    {
        var user = await GetUserByEmailAsync(input.Email);
        var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user);
        await _aevatarAccountEmailer.SendPasswordResetLinkAsync(user, resetToken);
    }
    
    private string GenerateVerificationCode()
    {
        var random = new Random();
        int code = random.Next(100000, 999999);
        return code.ToString();
    }
}