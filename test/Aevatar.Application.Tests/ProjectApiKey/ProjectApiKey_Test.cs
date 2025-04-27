using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aevatar.ApiKeys;
using Shouldly;
using Volo.Abp.Identity;
using Xunit;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;

namespace Aevatar.ProjectApiKey;

public abstract class ProjectApiKeyTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly Guid _projectId = Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703");
    private readonly ICurrentUser _currentUser;
    private readonly IdentityUserManager _identityUserManager;
    private readonly string _firstApiKeyName = "FirstApiKey";
    private readonly string _secondApiKeyName = "SecondApiKey";
    private readonly IProjectAppIdService _projectAppIdService;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public ProjectApiKeyTests()
    {
        _projectAppIdService = GetRequiredService<IProjectAppIdService>();
        _identityUserManager =GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task CreateApiKeyTest()
    {
        await _identityUserManager.CreateAsync(new IdentityUser(_currentUser.Id!.Value, "A", "2222@gmail.com"));
        
        await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value);
    }


    [Fact]
    public async Task CreateApiKeyExistTest()
    {
        await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value);
        
        await Assert.ThrowsAsync<BusinessException>(async () =>
            await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value));
    }

    [Fact]
    public async Task UpdateApiKeyNameTest()
    {
        using(_principalAccessor.Change(new []
              {
                  new Claim(AbpClaimTypes.UserId, _currentUser.Id!.Value.ToString()),
                  new Claim(AbpClaimTypes.UserName, _currentUser.UserName!),
                    new Claim(AbpClaimTypes.Email, _currentUser.Email!),
                    new Claim(AbpClaimTypes.Role, AevatarConsts.AdminRoleName)
              }))
        {
            await _identityUserManager.CreateAsync(new IdentityUser(_currentUser.Id!.Value, "A", "2222@gmail.com"));
            await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value);
            var keys = await _projectAppIdService.GetApiKeysAsync(_projectId);
            var exception = await Record.ExceptionAsync(async () =>
                await _projectAppIdService.ModifyApiKeyAsync(keys.First().Id, "bbbb"));
            Assert.Null(exception);
        }
    }

    [Fact]
    public async Task UpdateApiKeyNameExistTest()
    {
        using(_principalAccessor.Change(new []
              {
                  new Claim(AbpClaimTypes.UserId, _currentUser.Id!.Value.ToString()),
                  new Claim(AbpClaimTypes.UserName, _currentUser.UserName!),
                  new Claim(AbpClaimTypes.Email, _currentUser.Email!),
                  new Claim(AbpClaimTypes.Role, AevatarConsts.AdminRoleName)
              }))
        {
            await _identityUserManager.CreateAsync(new IdentityUser(_currentUser.Id!.Value, "A", "2222@gmail.com"));
            await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value);
            await _projectAppIdService.CreateAsync(_projectId, _secondApiKeyName, _currentUser.Id!.Value);
            var keys = await _projectAppIdService.GetApiKeysAsync(_projectId);
            var exception = await Record.ExceptionAsync(async () =>
                await _projectAppIdService.ModifyApiKeyAsync(keys.First().Id, _secondApiKeyName));
            exception.ShouldBeOfType<BusinessException>();
            exception.GetType().ShouldBe(typeof(BusinessException));
            exception.Message.ShouldBe("key name has exist");
        }
    }
}