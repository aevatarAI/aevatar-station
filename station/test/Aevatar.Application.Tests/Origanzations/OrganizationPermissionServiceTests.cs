using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Origanzations;

public abstract class OrganizationPermissionServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly IOrganizationPermissionService _organizationPermissionService;

    protected OrganizationPermissionServiceTests()
    {
        _organizationPermissionService = GetRequiredService<IOrganizationPermissionService>();
        _organizationService = GetRequiredService<IOrganizationService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
    }

    [Fact]
    public async Task Permission_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var createInput = new CreateOrganizationDto
        {
            DisplayName = "Test"
        };
        var organization = await _organizationService.CreateAsync(createInput);

        var permissions = await _organizationPermissionService.GetAsync(organization.Id, "R",
            OrganizationRoleHelper.GetRoleName(organization.Id, AevatarConsts.OrganizationReaderRoleName));
        permissions.Groups.Count.ShouldBe(1);
        permissions.Groups[0].Permissions.First(o=>o.Name == AevatarPermissions.Members.Manage).IsGranted.ShouldBeFalse();

        await _organizationPermissionService.UpdateAsync(organization.Id, "R",
            OrganizationRoleHelper.GetRoleName(organization.Id, AevatarConsts.OrganizationReaderRoleName),
            new UpdatePermissionsDto
            {
                Permissions = new[]
                {
                    new UpdatePermissionDto
                    {
                        Name = AevatarPermissions.Members.Manage,
                        IsGranted = true
                    }
                }
            });
    }
    
    [Fact]
    public async Task Permission_ModifyOwner_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var createInput = new CreateOrganizationDto
        {
            DisplayName = "Test"
        };
        var organization = await _organizationService.CreateAsync(createInput);
        
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _organizationPermissionService.UpdateAsync(organization.Id, "R", OrganizationRoleHelper.GetRoleName(organization.Id,AevatarConsts.OrganizationOwnerRoleName), new UpdatePermissionsDto
            {
                Permissions = new []{new UpdatePermissionDto
                {
                    Name = AevatarPermissions.Organizations.Default,
                    IsGranted = false
                }}
            }));
    }
}