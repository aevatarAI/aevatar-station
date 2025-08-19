using System;
using System.Threading.Tasks;
using Aevatar.Projects;
using Aevatar.Organizations;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Application.Tests.Projects;

public class ProjectServiceV2Tests : AevatarApplicationTestBase
{
    private readonly IProjectService _projectService;
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;

    public ProjectServiceV2Tests()
    {
        _projectService = GetRequiredService<IProjectService>();
        _organizationService = GetRequiredService<IOrganizationService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
    }

    [Fact]
    public async Task Create_Project_V2_With_Auto_Generated_Domain()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization V2"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectAutoDto()
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
    public async Task Create_Project_V2_With_Special_Characters_In_Name()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization V2"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectAutoDto()
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
    public async Task Create_Project_V2_With_Domain_Conflict_Should_Add_Suffix()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization V2"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        // 先创建一个项目占用基础域名
        var firstProjectInput = new CreateProjectAutoDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test App"
        };
        var firstProject = await _projectService.CreateProjectAsync(firstProjectInput);
        firstProject.DomainName.ShouldBe("testapp");

        // 再创建同名项目
        var secondProjectInput = new CreateProjectAutoDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test App"
        };

        // Act
        var secondProject = await _projectService.CreateProjectAsync(secondProjectInput);

        // Assert
        secondProject.DomainName.ShouldBe("testapp2"); // 应该添加后缀
    }

    [Fact]
    public async Task Create_Project_V2_With_Unicode_Characters()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization V2"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectAutoDto()
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
    public async Task Create_Project_V2_Empty_Display_Name_Should_Throw()
    {
        // Arrange
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));
        
        var createOrganizationInput = new CreateOrganizationDto
        {
            DisplayName = "Test Organization V2"
        };
        var organization = await _organizationService.CreateAsync(createOrganizationInput);

        var createProjectInput = new CreateProjectAutoDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "   " // 空白字符串
        };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () => 
            await _projectService.CreateProjectAsync(createProjectInput));
    }
}