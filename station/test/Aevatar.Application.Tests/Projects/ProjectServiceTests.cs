using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using Xunit;
using System.Security.Claims;
using Volo.Abp.Security.Claims;

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
    private readonly IProjectDomainRepository _domainRepository;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

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
        _domainRepository = GetRequiredService<IProjectDomainRepository>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
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
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe("testproject");

        project = await _projectService.GetProjectAsync(project.Id);
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe("testproject");
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
        ownerPermissions.Count.ShouldBe(22);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Projects.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Projects.Edit);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Members.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Members.Manage);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Create);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Edit);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Delete);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Roles.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Roles.Create);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Roles.Edit);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Roles.Delete);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Dashboard);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.LLMSModels.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.LLMSModels.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ProjectCorsOrigins.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ProjectCorsOrigins.Create);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ProjectCorsOrigins.Delete);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Plugins.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Plugins.Create);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Plugins.Edit);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Plugins.Delete);
        
        var readerRole = roles.Items.First(o => o.Name.EndsWith("Reader"));
        var readerPermissions =
            await _permissionManager.GetAllForRoleAsync(readerRole.Name);
        readerPermissions = readerPermissions.Where(o => o.IsGranted).ToList();
        readerPermissions.Count.ShouldBe(8);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Projects.Default);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Members.Default);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ApiKeys.Default);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Dashboard);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.LLMSModels.Default);
        readerPermissions.ShouldContain(o => o.Name == AevatarPermissions.LLMSModels.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.ProjectCorsOrigins.Default);
        ownerPermissions.ShouldContain(o => o.Name == AevatarPermissions.Plugins.Default);
    }

    [Fact]
    public async Task Project_Create_RepeatDomain_Test()
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
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe("testproject");
        
        await Should.ThrowAsync<UserFriendlyException>(async () => await  _projectService.CreateProjectAsync(createProjectInput));

        // Test with different display names that generate the same domain
        var secondProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test!!! Project"  // Still generates "testproject"
        };
        await Should.ThrowAsync<UserFriendlyException>(async () => await  _projectService.CreateProjectAsync(secondProjectInput));
        
        var thirdProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "TEST PROJECT"  // Still generates "testproject"
        };
        await Should.ThrowAsync<UserFriendlyException>(async () => await  _projectService.CreateProjectAsync(thirdProjectInput));
    }

    [Fact]
    public async Task Project_Create_InvalidDisplayName_Test()
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

        // Test with null display name - ABP Required attribute validation kicks in first
        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = null
        };
        await Should.ThrowAsync<AbpValidationException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with empty display name - ABP Required attribute validation kicks in first
        createProjectInput.DisplayName = "";
        await Should.ThrowAsync<AbpValidationException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with whitespace-only display name - ABP Required attribute validation kicks in first
        createProjectInput.DisplayName = "   ";
        await Should.ThrowAsync<AbpValidationException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with display name containing only special characters (no letters or digits)
        createProjectInput.DisplayName = "!@#$%^&*()";
        await Should.ThrowAsync<UserFriendlyException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with display name containing only spaces and special characters
        createProjectInput.DisplayName = "   !!!   ";
        await Should.ThrowAsync<UserFriendlyException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with display name containing problematic special characters for domain names
        createProjectInput.DisplayName = "Test@Project#Name$";
        await Should.ThrowAsync<UserFriendlyException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with display name containing Chinese characters or other unsupported characters
        createProjectInput.DisplayName = "测试项目";
        await Should.ThrowAsync<UserFriendlyException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with display name containing spaces (no longer allowed)
        createProjectInput.DisplayName = "Project Name";
        await Should.ThrowAsync<UserFriendlyException>(async () => await _projectService.CreateProjectAsync(createProjectInput));

        // Test with display name containing underscores (no longer allowed)
        createProjectInput.DisplayName = "Project_Name";
        await Should.ThrowAsync<UserFriendlyException>(async () => await _projectService.CreateProjectAsync(createProjectInput));
    }

    [Fact]
    public async Task Project_Create_ValidDisplayName_Test()
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

        // Test with valid display name containing letters and hyphens
        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Valid-Project-Name"
        };
        var project1 = await _projectService.CreateProjectAsync(createProjectInput);
        project1.ShouldNotBeNull();
        project1.DisplayName.ShouldBe("Valid-Project-Name");

        // Test with valid display name containing letters, numbers, and hyphens
        createProjectInput.DisplayName = "Project-123-Test";
        var project2 = await _projectService.CreateProjectAsync(createProjectInput);
        project2.ShouldNotBeNull();
        project2.DisplayName.ShouldBe("Project-123-Test");

        // Test with valid display name containing only letters and numbers
        createProjectInput.DisplayName = "Project123";
        var project3 = await _projectService.CreateProjectAsync(createProjectInput);
        project3.ShouldNotBeNull();
        project3.DisplayName.ShouldBe("Project123");
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
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);

        var updateInput = new UpdateProjectDto
        {
            DisplayName = "Test Project New"
        };
        await _projectService.UpdateAsync(project.Id, updateInput);
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.DisplayName.ShouldBe(updateInput.DisplayName);
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
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);
        
        var roles = await _projectService.GetRoleListAsync(project.Id);

        await _projectService.DeleteAsync(project.Id);

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await _projectService.GetAsync(project.Id));

        foreach (var role in roles.Items)
        {
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _roleManager.GetByIdAsync(role.Id));
        }

        var domain =
            await _domainRepository.FirstOrDefaultAsync(o => o.ProjectId == project.Id && o.IsDeleted == false);
        domain.DomainName.ShouldBe("testproject");
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
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);
        
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
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);
        
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
        
        var reader = new IdentityUser(Guid.NewGuid(), "reader", "reader@email.io");
        await _identityUserManager.CreateAsync(reader);

        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);
        
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
        
        await _projectService.SetMemberAsync(project.Id, new SetOrganizationMemberDto
        {
            Email = reader.Email,
            Join = true,
            RoleId = readerRole.Id
        });
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.MemberCount.ShouldBe(2);
        
        await Should.ThrowAsync<UserFriendlyException>(async () => await _organizationService.SetMemberAsync(organization.Id, new SetOrganizationMemberDto
        {
            Email = owner.Email,
            Join = false
        }));
        
        await _organizationService.SetMemberAsync(organization.Id, new SetOrganizationMemberDto
        {
            Email = reader.Email,
            Join = false
        });

        organization = await _organizationService.GetAsync(organization.Id);
        organization.MemberCount.ShouldBe(1);
        
        project = await _projectService.GetProjectAsync(project.Id);
        project.MemberCount.ShouldBe(1);


        owner = await _identityUserManager.GetByIdAsync(reader.Id);
        owner.IsInOrganizationUnit(organization.Id).ShouldBeFalse();
        owner.IsInOrganizationUnit(project.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Project_CreateDefault_Should_Create_Project_And_Assign_Current_User_As_Owner()
    {
        var email = "owner@email.io";
        using (_principalAccessor.Change(new[]
               {
                   new Claim(AbpClaimTypes.UserId, _currentUser.Id!.Value.ToString()),
                   new Claim(AbpClaimTypes.UserName, _currentUser.UserName!),
                   new Claim(AbpClaimTypes.Email, email)
               }))
        {
            await _identityUserManager.CreateAsync(new IdentityUser(_currentUser.Id!.Value, "owner", email));

            var organization = await _organizationService.CreateAsync(new CreateOrganizationDto
            {
                DisplayName = "Test Organization"
            });

            var project = await _projectService.CreateDefaultAsync(new CreateDefaultProjectDto
            {
                OrganizationId = organization.Id
            });

            project.DisplayName.ShouldBe("default project");
            project.DomainName.ShouldStartWith("defaultProject");
            project.DomainName.Length.ShouldBe("defaultProject".Length + 6);

            var roles = await _projectService.GetRoleListAsync(organization.Id);
            var ownerRole = roles.Items.First(o => o.Name.EndsWith("Owner"));

            var members = await _projectService.GetMemberListAsync(organization.Id, new GetOrganizationMemberListDto());
            members.Items.Count.ShouldBe(1);
            members.Items[0].Email.ShouldBe(email);
            members.Items[0].RoleId.ShouldBe(ownerRole.Id);
        }
    }

    [Fact]
    public async Task Project_CreateDefault_Should_Throw_When_Project_Exists()
    {
        var email = "owner@email.io";
        using (_principalAccessor.Change(new[]
               {
                   new Claim(AbpClaimTypes.UserId, _currentUser.Id!.Value.ToString()),
                   new Claim(AbpClaimTypes.UserName, _currentUser.UserName!),
                   new Claim(AbpClaimTypes.Email, email)
               }))
        {
            await _identityUserManager.CreateAsync(new IdentityUser(_currentUser.Id!.Value, "owner", email));

            var organization = await _organizationService.CreateAsync(new CreateOrganizationDto
            {
                DisplayName = "Test Organization"
            });

            var first = await _projectService.CreateDefaultAsync(new CreateDefaultProjectDto
            {
                OrganizationId = organization.Id
            });
            first.ShouldNotBeNull();

            await Should.ThrowAsync<UserFriendlyException>(async () =>
                await _projectService.CreateDefaultAsync(new CreateDefaultProjectDto
                {
                    OrganizationId = organization.Id
                }));
        }
    }

    [Fact]
    public async Task Project_Auto_Create_Test()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization Auto"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "My Awesome App"
        };

        // Act
        var project = await _projectService.CreateProjectAsync(createProjectInput);

        // Assert
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe("myawesomeapp"); // 应该自动生成
        
        // 验证项目详情
        var projectDetails = await _projectService.GetProjectAsync(project.Id);
        projectDetails.DisplayName.ShouldBe(createProjectInput.DisplayName);
        projectDetails.DomainName.ShouldBe("myawesomeapp");
        projectDetails.MemberCount.ShouldBe(0);
    }

    [Fact]
    public async Task Project_Auto_Create_SpecialCharacters_Test()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization Auto"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "My App@#$%^&*()123!"
        };

        // Act
        var project = await _projectService.CreateProjectAsync(createProjectInput);

        // Assert
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe("myapp123"); // 特殊字符应该被过滤
    }

    [Fact]
    public async Task Project_Auto_Create_DomainConflict_Test()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization Auto"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        // 先创建一个项目占用基础域名
        var firstProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test App"
        };
        var firstProject = await _projectService.CreateProjectAsync(firstProjectInput);
        firstProject.DomainName.ShouldBe("testapp");

        // 再创建同名项目
        var secondProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test App"
        };

        // Act & Assert - 应该抛出域名已存在的异常
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _projectService.CreateProjectAsync(secondProjectInput));
        
        exception.Message.ShouldContain("testapp");
        exception.Message.ShouldContain("already exists");
    }

    [Fact]
    public async Task Project_Auto_Create_Unicode_Test()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization Auto"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "中文项目App123"
        };

        // Act
        var project = await _projectService.CreateProjectAsync(createProjectInput);

        // Assert
        project.DisplayName.ShouldBe(createProjectInput.DisplayName);
        project.DomainName.ShouldBe("app123"); // 只保留英文字母和数字
    }

    [Fact]
    public async Task Project_Auto_Create_EmptyName_Test()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization Auto"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "   " // 空白字符串
        };

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () => 
            await _projectService.CreateProjectAsync(createProjectInput));
    }

    [Fact]
    public async Task Project_Recent_Used_Test()
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
            DisplayName = "Test Project"
        };
        var project = await _projectService.CreateProjectAsync(createProjectInput);
        await _projectService.SaveRecentUsedProjectAsync(new RecentUsedProjectDto()
        {
            OrganizationId = organization.Id,
            ProjectId = project.Id
        });
        var recentUsedProject = await _projectService.GetRecentUsedProjectAsync();
        recentUsedProject.OrganizationId.ShouldBe(organization.Id);
        recentUsedProject.ProjectId.ShouldBe(project.Id);
        
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _projectService.SaveRecentUsedProjectAsync(new RecentUsedProjectDto()
            {
                OrganizationId = organization.Id,
                ProjectId = Guid.NewGuid()
            }));
        
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _projectService.SaveRecentUsedProjectAsync(new RecentUsedProjectDto()
            {
                OrganizationId = Guid.NewGuid(),
                ProjectId = project.Id
            }));
    }

    [Fact]
    public async Task Organization_Create_With_Default_Project_Should_Create_And_Assign_Owner()
    {
        var email = "owner2@email.io";
        using (_principalAccessor.Change(new[]
               {
                   new Claim(AbpClaimTypes.UserId, _currentUser.Id!.Value.ToString()),
                   new Claim(AbpClaimTypes.UserName, _currentUser.UserName!),
                   new Claim(AbpClaimTypes.Email, email)
               }))
        {
            await _identityUserManager.CreateAsync(new IdentityUser(_currentUser.Id!.Value, "owner2", email));

            var result = await _projectService.CreateOrgWithDefaultProjectAsync(new CreateOrganizationDto
            {
                DisplayName = "Org With Default"
            });

            result.ShouldNotBeNull();
            result.DisplayName.ShouldBe("Org With Default");
            result.Project.ShouldNotBeNull();
            result.Project.DisplayName.ShouldBe("default project");
            result.Project.DomainName.ShouldStartWith("defaultProject");
            result.Project.DomainName.Length.ShouldBe("defaultProject".Length + 6);

            var roles = await _projectService.GetRoleListAsync(result.Id);
            var ownerRole = roles.Items.First(o => o.Name.EndsWith("Owner"));
            
            var members = await _projectService.GetMemberListAsync(result.Id, new GetOrganizationMemberListDto());
            members.Items.Count.ShouldBe(1);
            members.Items[0].Email.ShouldBe(email);
            members.Items[0].RoleId.ShouldBe(ownerRole.Id);
        }
    }
}