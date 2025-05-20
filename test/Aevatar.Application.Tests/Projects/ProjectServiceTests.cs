using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Shouldly;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Projects;

public abstract class ProjectServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IProjectService _projectService;
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly OrganizationUnitManager _organizationUnitManager;
    private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;

    protected ProjectServiceTests()
    {
        _organizationUnitManager = GetRequiredService<OrganizationUnitManager>();
        _organizationUnitRepository = GetRequiredService<IRepository<OrganizationUnit, Guid>>();
        _roleManager = GetRequiredService<IdentityRoleManager>();
        _projectService = GetRequiredService<IProjectService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _permissionManager = GetRequiredService<IPermissionManager>();
        _organizationService = GetRequiredService<IOrganizationService>();
    }

    [Fact]
    public async Task Project_Create_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project",
            DomainName = "App"
        };
        var project = await _projectService.CreateAsync(createProjectInput);
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe(createProjectInput.DomainName);

        project = await _projectService.GetProjectAsync(project.Id);
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe(createProjectInput.DomainName);
        project.MemberCount.ShouldBe(0);
        project.CreationTime.ShouldBeGreaterThan(0);

        var projects = await _projectService.GetListAsync(new GetProjectListDto
        {
            OrganizationId = organization.Id
        });
        projects.Items.Count.ShouldBe(1);
        projects.Items[0].DisplayName.ShouldBe(createProjectInput.DisplayName);

        var roles = await _projectService.GetRoleListAsync(project.Id);
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
        readerPermissions.Count.ShouldBe(3);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Organizations.Default);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.OrganizationMembers.Default);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Default);
    }

    [Fact]
    public async Task Project_Update_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project",
            DomainName = "App"
        };
        var project = await _projectService.CreateAsync(createProjectInput);

        var updateInput = new UpdateProjectDto
        {
            DisplayName = "Test Project New",
            DomainName = "App New"
        };
        await _projectService.UpdateAsync(project.Id, updateInput);
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.DisplayName.ShouldBe(updateInput.DisplayName);
        project.DomainName.ShouldBe(updateInput.DomainName);
    }

    [Fact]
    public async Task Project_Delete_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project",
            DomainName = "App"
        };
        var project = await _projectService.CreateAsync(createProjectInput);
        
        var roles = await _projectService.GetRoleListAsync(project.Id);

        await _projectService.DeleteAsync(project.Id);

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await _projectService.GetAsync(project.Id));

        foreach (var role in roles.Items)
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _roleManager.GetByIdAsync(role.Id));
        }
    }

    [Fact]
    public async Task Project_SetMember_Test()
    {
        var owner = new IdentityUser(_currentUser.Id.Value, "owner", "owner@email.io");
        await _identityUserManager.CreateAsync(owner);

        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project",
            DomainName = "App"
        };
        var project = await _projectService.CreateAsync(createProjectInput);
        
        var roles = await _projectService.GetRoleListAsync(project.Id);
        var ownerRole = roles.Items.First(o => o.Name.EndsWith("Owner"));
        var readerRole = roles.Items.First(o => o.Name.EndsWith("Reader"));
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.MemberCount.ShouldBe(0);

        var members =
            await _projectService.GetMemberListAsync(project.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(0);

        var readerUser = new IdentityUser(Guid.NewGuid(), "reader", "reader@email.io");
        await _identityUserManager.CreateAsync(readerUser);

        await _projectService.SetMemberAsync(project.Id, new SetOrganizationMemberDto
        {
            Email = readerUser.Email,
            Join = true,
            RoleId = readerRole.Id
        });
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.MemberCount.ShouldBe(1);
        
        members =
            await _projectService.GetMemberListAsync(project.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(1);
        members.Items[0].UserName.ShouldBe(readerUser.UserName);
        members.Items[0].Email.ShouldBe(readerUser.Email);
        members.Items[0].RoleId.ShouldBe(readerRole.Id);

        await _projectService.SetMemberRoleAsync(project.Id, new SetOrganizationMemberRoleDto
        {
            UserId = readerUser.Id,
            RoleId = ownerRole.Id
        });
        
        members =
            await _projectService.GetMemberListAsync(project.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(1);
        members.Items[0].RoleId.ShouldBe(ownerRole.Id);
        
        await _projectService.SetMemberAsync(project.Id, new SetOrganizationMemberDto
        {
            Email = readerUser.Email,
            Join = false
        });
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.MemberCount.ShouldBe(0);

        members =
            await _projectService.GetMemberListAsync(project.Id, new GetOrganizationMemberListDto());
        members.Items.Count.ShouldBe(0);

        readerUser = await _identityUserManager.GetByIdAsync(readerUser.Id);
        readerUser.IsInOrganizationUnit(project.Id).ShouldBeFalse();
    }
    
    [Fact]
    public async Task Organization_Delete_WithProject_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project",
            DomainName = "App"
        };
        var project = await _projectService.CreateAsync(createProjectInput);
        
        var organizationRoles = await _organizationService.GetRoleListAsync(organization.Id);
        var projectRoles = await _projectService.GetRoleListAsync(project.Id);

        await _organizationService.DeleteAsync(organization.Id);

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await _organizationService.GetAsync(organization.Id));

        foreach (var role in organizationRoles.Items)
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _roleManager.GetByIdAsync(role.Id));
        }
        
        var user = await _identityUserManager.GetByIdAsync(_currentUser.Id.Value);
        user.IsInOrganizationUnit(organization.Id).ShouldBeFalse();
        
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await _projectService.GetProjectAsync(project.Id));

        foreach (var role in projectRoles.Items)
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _roleManager.GetByIdAsync(role.Id));
        }
    }
    
    [Fact]
    public async Task Organization_DeleteMember_WithProject_Test()
    {
        var owner = new IdentityUser(_currentUser.Id.Value, "owner", "owner@email.io");
        await _identityUserManager.CreateAsync(owner);

        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project",
            DomainName = "App"
        };
        var project = await _projectService.CreateAsync(createProjectInput);
        
        var roles = await _projectService.GetRoleListAsync(project.Id);
        var ownerRole = roles.Items.First(o => o.Name.EndsWith("Owner"));
        var readerRole = roles.Items.First(o => o.Name.EndsWith("Reader"));

        await _projectService.SetMemberAsync(project.Id, new SetOrganizationMemberDto
        {
            Email = owner.Email,
            Join = true,
            RoleId = ownerRole.Id
        });
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.MemberCount.ShouldBe(1);

        
        await _organizationService.SetMemberAsync(organization.Id, new SetOrganizationMemberDto
        {
            Email = owner.Email,
            Join = false
        });

        organization = await _organizationService.GetAsync(organization.Id);
        organization.MemberCount.ShouldBe(0);
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.MemberCount.ShouldBe(0);


        owner = await _identityUserManager.GetByIdAsync(owner.Id);
        owner.IsInOrganizationUnit(organization.Id).ShouldBeFalse();
        owner.IsInOrganizationUnit(project.Id).ShouldBeFalse();
    }
}