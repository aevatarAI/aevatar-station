using System;
using System.Threading.Tasks;
using Aevatar.ApiKeys;
using Aevatar.Organizations;
using Aevatar.Projects;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.ApiRequests;

public abstract class ApiRequestServiceTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IProjectService _projectService;
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;
    private readonly IApiRequestProvider _apiRequestProvider;
    private readonly IApiRequestService _apiRequestService;
    private readonly IProjectAppIdService _projectAppIdService;

    protected ApiRequestServiceTests()
    {
        _projectService = GetRequiredService<IProjectService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _organizationService = GetRequiredService<IOrganizationService>();
        _apiRequestProvider = GetRequiredService<IApiRequestProvider>();
        _projectAppIdService = GetRequiredService<IProjectAppIdService>();
        _apiRequestService = GetRequiredService<IApiRequestService>();
    }

    [Fact]
    public async Task ApiRequest_Test()
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
            DisplayName = "Test Project1",
            DomainName = "App"
        };
        var project1 = await _projectService.CreateAsync(createProjectInput);
        
        var createProjectInput2 = new CreateProjectDto()
        {
            OrganizationId = organization.Id,
            DisplayName = "Test Project2",
            DomainName = "App2"
        };
        var project2 = await _projectService.CreateAsync(createProjectInput2);

        await _projectAppIdService.CreateAsync(project1.Id, "TestKey1", _currentUser.Id);
        var apps = await _projectAppIdService.GetApiKeysAsync(project1.Id);
        var appId1 = apps[0].AppId;
        
        await _projectAppIdService.CreateAsync(project2.Id, "TestKey2", _currentUser.Id);
        apps = await _projectAppIdService.GetApiKeysAsync(project2.Id);
        var appId2 = apps[0].AppId;

        var startTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        
        await _apiRequestProvider.IncreaseRequestAsync(appId1, startTime.AddMinutes(1).AddSeconds(1));
        await _apiRequestProvider.IncreaseRequestAsync(appId1, startTime.AddHours(2).AddMinutes(2).AddSeconds(2));
        await _apiRequestProvider.IncreaseRequestAsync(appId2, startTime.AddHours(2).AddMinutes(3).AddSeconds(3));

        await _apiRequestProvider.FlushAsync();
        
        await _apiRequestProvider.IncreaseRequestAsync(appId1, startTime.AddMinutes(11).AddSeconds(1));
        await _apiRequestProvider.IncreaseRequestAsync(appId1, startTime.AddHours(2).AddMinutes(12).AddSeconds(2));
        await _apiRequestProvider.IncreaseRequestAsync(appId2, startTime.AddHours(2).AddMinutes(13).AddSeconds(3));

        await _apiRequestProvider.FlushAsync();

        var apiRequests = await _apiRequestService.GetListAsync(new GetApiRequestDto
        {
            OrganizationId = organization.Id,
            StartTime = DateTimeHelper.ToUnixTimeMilliseconds(startTime),
            EndTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
        });
        
        apiRequests.Items.Count.ShouldBe(2);
        apiRequests.Items[0].Count.ShouldBe(2);
        apiRequests.Items[0].Time.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(startTime));
        apiRequests.Items[1].Count.ShouldBe(4);
        apiRequests.Items[1].Time.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(startTime.AddHours(2)));
        
        apiRequests = await _apiRequestService.GetListAsync(new GetApiRequestDto
        {
            OrganizationId = project1.Id,
            StartTime = DateTimeHelper.ToUnixTimeMilliseconds(startTime),
            EndTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
        });
        
        apiRequests.Items.Count.ShouldBe(2);
        apiRequests.Items[0].Count.ShouldBe(2);
        apiRequests.Items[0].Time.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(startTime));
        apiRequests.Items[1].Count.ShouldBe(2);
        apiRequests.Items[1].Time.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(startTime.AddHours(2)));
        
        apiRequests = await _apiRequestService.GetListAsync(new GetApiRequestDto
        {
            OrganizationId = project2.Id,
            StartTime = DateTimeHelper.ToUnixTimeMilliseconds(startTime),
            EndTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
        });
        
        apiRequests.Items.Count.ShouldBe(1);
        apiRequests.Items[0].Count.ShouldBe(2);
        apiRequests.Items[0].Time.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(startTime.AddHours(2)));
    }
}