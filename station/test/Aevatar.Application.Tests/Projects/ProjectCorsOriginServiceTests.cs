using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Projects;

public abstract class ProjectCorsOriginServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly IProjectCorsOriginService _projectCorsOriginService;

    protected ProjectCorsOriginServiceTests()
    {
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _projectCorsOriginService = GetRequiredService<IProjectCorsOriginService>();
    }
    
    [Fact]
    public async Task CorsOrigin_Create_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var projectId = Guid.NewGuid();
        var domain1 = "http://domain1.com";
        var domain2 = "http://domain2.com";

        var result = await _projectCorsOriginService.CreateAsync(projectId, new CreateProjectCorsOriginDto
        {
            Domain = domain1
        });
        result.ProjectId.ShouldBe(projectId);
        result.Domain.ShouldBe(domain1);

        var list = await _projectCorsOriginService.GetListAsync(Guid.NewGuid());
        list.Items.Count.ShouldBe(0);
        
        list = await _projectCorsOriginService.GetListAsync(projectId);
        list.Items.Count.ShouldBe(1);
        list.Items[0].ProjectId.ShouldBe(projectId);
        list.Items[0].Domain.ShouldBe(domain1);
        list.Items[0].CreatorName.ShouldBe("test");
        list.Items[0].CreationTime.ShouldBeGreaterThan(0);
        
        result = await _projectCorsOriginService.CreateAsync(projectId, new CreateProjectCorsOriginDto
        {
            Domain = domain2
        });
        result.ProjectId.ShouldBe(projectId);
        result.Domain.ShouldBe(domain2);
        
        list = await _projectCorsOriginService.GetListAsync(projectId);
        list.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CorsOrigin_Delete_Test()
    {
        await _identityUserManager.CreateAsync(
            new IdentityUser(
                _currentUser.Id.Value,
                "test",
                "test@email.io"));

        var projectId = Guid.NewGuid();
        
        var domain = "http://domain.com";

        var result = await _projectCorsOriginService.CreateAsync(projectId, new CreateProjectCorsOriginDto
        {
            Domain = domain
        });
        
        var list = await _projectCorsOriginService.GetListAsync(projectId);
        list.Items.Count.ShouldBe(1);

        await _projectCorsOriginService.DeleteAsync(projectId, result.Id);
        
        list = await _projectCorsOriginService.GetListAsync(projectId);
        list.Items.Count.ShouldBe(0);
    }
}