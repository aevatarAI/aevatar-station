using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Projects;
using Shouldly;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Origanzations;

public abstract class OrganizationServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly OrganizationUnitManager _organizationUnitManager;
    private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;

    protected OrganizationServiceTests()
    {
        _organizationUnitManager = GetRequiredService<OrganizationUnitManager>();
        _organizationUnitRepository = GetRequiredService<IRepository<OrganizationUnit, Guid>>();
        _roleManager = GetRequiredService<IdentityRoleManager>();
        _organizationService = GetRequiredService<IOrganizationService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _permissionManager = GetRequiredService<IPermissionManager>();
    }

    [Fact]
    public async Task Organization_Create_Test()
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
        organization.DisplayName.ShouldBe(createInput.DisplayName);

        organization = await _organizationService.GetAsync(organization.Id);
        organization.DisplayName.ShouldBe(createInput.DisplayName);
        organization.MemberCount.ShouldBe(1);
        organization.CreationTime.ShouldBeGreaterThan(0);

        var organizations = await _organizationService.GetListAsync(new GetOrganizationListDto());
        organizations.Items.Count.ShouldBe(1);
        organizations.Items[0].DisplayName.ShouldBe(createInput.DisplayName);

        var roles = await _organizationService.GetRoleListAsync(organization.Id);
        roles.Items.Count.ShouldBe(2);
        roles.Items.ShouldContain(o => o.Name.EndsWith("Owner"));
        roles.Items.ShouldContain(o => o.Name.EndsWith("Reader"));

        var ownerRole = roles.Items.First(o => o.Name.EndsWith("Owner"));
        var ownerPermissions =
            await _permissionManager.GetAllForRoleAsync(ownerRole.Name);
        ownerPermissions = ownerPermissions.Where(o => o.IsGranted).ToList();
        ownerPermissions.Count.ShouldBe(10);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Organizations.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Organizations.Create);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Organizations.Edit);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Organizations.Delete);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.OrganizationMembers.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.OrganizationMembers.Manage);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Create);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Edit);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Delete);

        var readerRole = roles.Items.First(o => o.Name.EndsWith("Reader"));
        var readerPermissions =
            await _permissionManager.GetAllForRoleAsync(readerRole.Name);
        readerPermissions = readerPermissions.Where(o => o.IsGranted).ToList();
        readerPermissions.Count.ShouldBe(2);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Organizations.Default);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.OrganizationMembers.Default);

        var user = await _identityUserManager.GetByIdAsync(_currentUser.Id.Value);
        var isInRole = await _identityUserManager.IsInRoleAsync(user, ownerRole.Name);
        isInRole.ShouldBeTrue();
        
        isInRole = await _identityUserManager.IsInRoleAsync(user, readerRole.Name);
        isInRole.ShouldBeFalse();
        
        user.IsInOrganizationUnit(organization.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task Organization_Update_Test()
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
        organization.DisplayName.ShouldBe(createInput.DisplayName);

        var updateInput = new UpdateOrganizationDto
        {
            DisplayName = "Test New"
        };
        await _organizationService.UpdateAsync(organization.Id, updateInput);
        
        organization = await _organizationService.GetAsync(organization.Id);
        organization.DisplayName.ShouldBe(updateInput.DisplayName);
    }

    [Fact]
    public async Task Organization_Delete_Test()
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
        
        var roles = await _organizationService.GetRoleListAsync(organization.Id);

        await _organizationService.DeleteAsync(organization.Id);

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await _organizationService.GetAsync(organization.Id));

        foreach (var role in roles.Items)
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _roleManager.GetByIdAsync(role.Id));
        }
        
        var user = await _identityUserManager.GetByIdAsync(_currentUser.Id.Value);
        user.IsInOrganizationUnit(organization.Id).ShouldBeFalse();
    }
    
    [Fact]
    public async Task Organization_SetMember_Test()
    {
        var owner = new IdentityUser(_currentUser.Id.Value, "owner", "owner@email.io");
        await _identityUserManager.CreateAsync(owner);

        var createInput = new CreateOrganizationDto
        {
            DisplayName = "Test"
        };
        var organization = await _organizationService.CreateAsync(createInput);
        
        var roles = await _organizationService.GetRoleListAsync(organization.Id);
        var ownerRole = roles.Items.First(o => o.Name.EndsWith("Owner"));
        var readerRole = roles.Items.First(o => o.Name.EndsWith("Reader"));
        
        organization = await _organizationService.GetAsync(organization.Id);
        organization.MemberCount.ShouldBe(1);

        var members =
            await _organizationService.GetMemberListAsync(organization.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(1);
        members.Items[0].UserName.ShouldBe(owner.UserName);
        members.Items[0].Email.ShouldBe(owner.Email);
        members.Items[0].RoleId.ShouldBe(ownerRole.Id);

        var readerUser = new IdentityUser(Guid.NewGuid(), "reader", "reader@email.io");
        await _identityUserManager.CreateAsync(readerUser);

        await _organizationService.SetMemberAsync(organization.Id, new SetOrganizationMemberDto
        {
            Email = readerUser.Email,
            Join = true,
            RoleId = readerRole.Id
        });
        
        organization = await _organizationService.GetAsync(organization.Id);
        organization.MemberCount.ShouldBe(2);
        
        members =
            await _organizationService.GetMemberListAsync(organization.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(2);
        var readerMember = members.Items.First(o => o.Id == readerUser.Id);
        readerMember.UserName.ShouldBe(readerUser.UserName);
        readerMember.Email.ShouldBe(readerUser.Email);
        readerMember.RoleId.ShouldBe(readerRole.Id);

        await _organizationService.SetMemberRoleAsync(organization.Id, new SetOrganizationMemberRoleDto
        {
            UserId = readerUser.Id,
            RoleId = ownerRole.Id
        });
        
        members =
            await _organizationService.GetMemberListAsync(organization.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(2);
        readerMember = members.Items.First(o => o.Id == readerUser.Id);
        readerMember.RoleId.ShouldBe(ownerRole.Id);
        
        await _organizationService.SetMemberAsync(organization.Id, new SetOrganizationMemberDto
        {
            Email = readerUser.Email,
            Join = false
        });
        
        organization = await _organizationService.GetAsync(organization.Id);
        organization.MemberCount.ShouldBe(1);

        members =
            await _organizationService.GetMemberListAsync(organization.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(1);

        readerUser = await _identityUserManager.GetByIdAsync(readerUser.Id);
        readerUser.IsInOrganizationUnit(organization.Id).ShouldBeFalse();
    }
}