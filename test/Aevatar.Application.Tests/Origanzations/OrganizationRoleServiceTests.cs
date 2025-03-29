using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Origanzations;

public abstract class OrganizationRoleServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly OrganizationUnitManager _organizationUnitManager;
    private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IOrganizationRoleService _organizationRoleService;
    
    protected OrganizationRoleServiceTests()
    {
        _organizationUnitManager = GetRequiredService<OrganizationUnitManager>();
        _organizationUnitRepository = GetRequiredService<IRepository<OrganizationUnit, Guid>>();
        _roleManager = GetRequiredService<IdentityRoleManager>();
        _organizationService = GetRequiredService<IOrganizationService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _permissionManager = GetRequiredService<IPermissionManager>();
        _organizationRoleService = GetRequiredService<IOrganizationRoleService>();
    }
    
    [Fact]
    public async Task Role_Test()
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

        var roles = await _organizationRoleService.GetListAsync(organization.Id);
        roles.Items.Count.ShouldBe(2);

        var createRoleInput = new IdentityRoleCreateDto
        {
            Name = "Dev"
        };
        var role = await _organizationRoleService.CreateAsync(organization.Id, createRoleInput);
        
        roles = await _organizationRoleService.GetListAsync(organization.Id);
        roles.Items.Count.ShouldBe(3);
        roles.Items.First(o =>
            o.Id == role.Id).Name.ShouldBe(OrganizationRoleHelper.GetRoleName(organization.Id, createRoleInput.Name));
        
        var updateRoleInput = new IdentityRoleUpdateDto
        {
            Name = "Dev-2"
        };
        await _organizationRoleService.UpdateAsync(organization.Id, role.Id, updateRoleInput);

        roles = await _organizationRoleService.GetListAsync(organization.Id);
        roles.Items.Count.ShouldBe(3);
        roles.Items.First(o =>
            o.Id == role.Id).Name.ShouldBe(OrganizationRoleHelper.GetRoleName(organization.Id, updateRoleInput.Name));

        await _organizationRoleService.DeleteAsync(organization.Id, role.Id);

        roles = await _organizationRoleService.GetListAsync(organization.Id);
        roles.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Role_ModifyOwner_Test()
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
        
        var roles = await _organizationRoleService.GetListAsync(organization.Id);
        var ownerRole = roles.Items.First(o => o.Name.EndsWith(AevatarConsts.OrganizationOwnerRoleName));

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _organizationRoleService.UpdateAsync(organization.Id, ownerRole.Id, new IdentityRoleUpdateDto
            {
                Name = "NewName"
            }));
        
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _organizationRoleService.DeleteAsync(organization.Id, ownerRole.Id));
    }
    
    [Fact]
    public async Task Role_WrongOrganization_Test()
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
        var wrongOrganizationId = Guid.NewGuid();
        
        var roles = await _organizationRoleService.GetListAsync(organization.Id);
        var readerRole = roles.Items.First(o => o.Name.EndsWith(AevatarConsts.OrganizationReaderRoleName));

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _organizationRoleService.UpdateAsync(wrongOrganizationId, readerRole.Id, new IdentityRoleUpdateDto
            {
                Name = "NewName"
            }));
        
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _organizationRoleService.DeleteAsync(wrongOrganizationId, readerRole.Id));
    }
}