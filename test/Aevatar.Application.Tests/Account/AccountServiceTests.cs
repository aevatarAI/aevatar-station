using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Caching;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace Aevatar.Account;

public abstract class AccountServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IAccountService _accountService;
    private readonly IDistributedCache<string,string> _registerCode;
    private readonly IdentityUserManager _identityUserManager;

    protected AccountServiceTests()
    {
        _accountService = GetRequiredService<IAccountService>();
        _registerCode = GetRequiredService<IDistributedCache<string,string>>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
    }

    [Fact]
    public async Task Register_Test()
    {
        var email = "test@email.io";

        var user = await _identityUserManager.FindByEmailAsync(email);
        user.ShouldBeNull();
        
        await _accountService.SendRegisterCodeAsync(new SendRegisterCodeDto
        {
            Email = email,
            AppName = "Aevatar"
        });

        var code = await _registerCode.GetAsync($"RegisterCode_{email.ToLower()}");

        var registerInput = new AevatarRegisterDto
        {
            AppName = "Aevatar",
            Code = "Worng",
            Password = "Abc@123456",
            EmailAddress = email,
            UserName = "Tester"
        };
        
        await Should.ThrowAsync<UserFriendlyException>(async ()=> await _accountService.RegisterAsync(registerInput));

        registerInput.Code = code;
        await _accountService.RegisterAsync(registerInput);
        
        user = await _identityUserManager.FindByEmailAsync(email);
        user.UserName.ShouldBe(registerInput.UserName);
        user.Email.ShouldBe(registerInput.EmailAddress);

        var checkPassword = await _identityUserManager.CheckPasswordAsync(user, registerInput.Password);
        checkPassword.ShouldBeTrue();
        
        await Should.ThrowAsync<UserFriendlyException>(async ()=> await _accountService.SendRegisterCodeAsync(new SendRegisterCodeDto
        {
            Email = email,
            AppName = "Aevatar"
        }));
    }

    [Fact]
    public async Task ResetPassword_Test()
    {
        var user = new IdentityUser(Guid.NewGuid(), "test", "test@email.io");
        await _identityUserManager.CreateAsync(user);

        await _accountService.SendPasswordResetCodeAsync(new SendPasswordResetCodeDto
        {
            Email = user.Email,
            AppName = "Aevatar"
        });
        
        var verifyResult = await _accountService.VerifyPasswordResetTokenAsync(new  VerifyPasswordResetTokenInput()
        {
            UserId = user.Id,
            ResetToken = "wrong token",
        });
        verifyResult.ShouldBeFalse();
        
        await Should.ThrowAsync<AbpIdentityResultException>(async ()=> await _accountService.ResetPasswordAsync(new ResetPasswordDto()
        {
            UserId = user.Id,
            Password = "Abc@123",
            ResetToken = "wrong token"
        }));
    }
}