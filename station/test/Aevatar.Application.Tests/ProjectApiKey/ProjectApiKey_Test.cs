using System;
<<<<<<< HEAD
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Aevatar.ApiKeys;
using Aevatar.Common;
using Aevatar.Organizations;
using Aevatar.Service;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Volo.Abp.Identity;
using Xunit;
using Moq;
using NSubstitute;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.ProjectApiKey;

public sealed class ProjectApiKey_Test : AevatarApplicationTestBase
{
    private readonly IProjectAppIdService _projectApiKeyService;
    private readonly Guid _projectId = Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703");
    private readonly Guid _currentUserId = Guid.Parse("ca63293c-fdde-4730-b10b-e95c37379702");
    private readonly Mock<IProjectAppIdRepository> _apiKeysRepository;
    private readonly Mock<IdentityUserManager> _identityUserManager;
    private readonly Mock<ILogger<ProjectAppIdService>> _logger;
    private readonly Guid _firstApikeyId = Guid.Parse("df63293c-fdde-4730-b10b-e95c37379732");
    private readonly string _firstApiKeyName = "FirstApiKey";
    private readonly Guid _secondApikeyId = Guid.Parse("cd63293c-fdde-4730-b10b-e95c3737973d");
    private readonly string _appSecretKey = "cd63293c-fdde-4730-b10b-e95c37379252";
    private readonly string _secondApiKeyName = "SecondApiKey";
    private readonly CancellationToken _cancellationToken = new CancellationToken();
    private readonly ProjectAppIdInfo _projectAppIdInfo;
    private readonly Mock<IOrganizationPermissionChecker> _organizationPermissionChecker;
    private readonly Mock<IUserAppService> _userAppService;

    public ProjectApiKey_Test()
    {
        _apiKeysRepository = new Mock<IProjectAppIdRepository>();
        _identityUserManager = new Mock<IdentityUserManager>();
        _userAppService = new Mock<IUserAppService>();
        _organizationPermissionChecker = new Mock<IOrganizationPermissionChecker>();
        _logger = new Mock<ILogger<ProjectAppIdService>>();
=======
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
>>>>>>> origin/dev
    }

    [Fact]
    public async Task CreateApiKeyTest()
    {
<<<<<<< HEAD
        var apiKeyInfo = new ProjectAppIdInfo(_firstApikeyId, _projectId, _firstApiKeyName, _appSecretKey, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.InsertAsync(apiKeyInfo, false, _cancellationToken))
            .ReturnsAsync(apiKeyInfo);
        _apiKeysRepository.Setup(s => s.CheckProjectAppNameExist(_projectId, _firstApiKeyName)).ReturnsAsync(false);

        _identityUserManager.Setup(s => s.GetByIdAsync(_currentUserId))
            .ReturnsAsync(new IdentityUser(_currentUserId, "A", "2222@gmail.com"));

        var projectApiKeyService = new ProjectAppIdService(_apiKeysRepository.Object, _logger.Object,
            _organizationPermissionChecker.Object, _userAppService.Object, _identityUserManager.Object);
        await projectApiKeyService.CreateAsync(_projectId, _firstApiKeyName, _currentUserId);
=======
        await _identityUserManager.CreateAsync(new IdentityUser(_currentUser.Id!.Value, "A", "2222@gmail.com"));
        
        await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value);
>>>>>>> origin/dev
    }


    [Fact]
    public async Task CreateApiKeyExistTest()
    {
<<<<<<< HEAD
        var apiKeyInfo = new ProjectAppIdInfo(_firstApikeyId, _projectId, _firstApiKeyName, _appSecretKey, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.InsertAsync(apiKeyInfo, false, _cancellationToken))
            .ReturnsAsync(apiKeyInfo);

        _apiKeysRepository.Setup(s => s.CheckProjectAppNameExist(_projectId, _firstApiKeyName)).ReturnsAsync(true);
        _identityUserManager.Setup(s => s.GetByIdAsync(_currentUserId))
            .ReturnsAsync(new IdentityUser(_currentUserId, "A", "2222@gmail.com"));

        var projectApiKeyService = new ProjectAppIdService(_apiKeysRepository.Object, _logger.Object,
            _organizationPermissionChecker.Object, _userAppService.Object, _identityUserManager.Object);

        await Assert.ThrowsAsync<BusinessException>(async () =>
            await projectApiKeyService.CreateAsync(_projectId, _firstApiKeyName, _currentUserId));
=======
        await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value);
        
        await Assert.ThrowsAsync<BusinessException>(async () =>
            await _projectAppIdService.CreateAsync(_projectId, _firstApiKeyName, _currentUser.Id!.Value));
>>>>>>> origin/dev
    }

    [Fact]
    public async Task UpdateApiKeyNameTest()
    {
<<<<<<< HEAD
        var apiKeyInfo = new ProjectAppIdInfo(_firstApikeyId, _projectId, _firstApiKeyName, _appSecretKey, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.GetAsync(_firstApikeyId)).ReturnsAsync(apiKeyInfo);
        var projectApiKeyService = new ProjectAppIdService(_apiKeysRepository.Object, _logger.Object,
            _organizationPermissionChecker.Object, _userAppService.Object, _identityUserManager.Object);

        var exception = await Record.ExceptionAsync(async () =>
            await projectApiKeyService.ModifyApiKeyAsync(_firstApikeyId, "bbbb"));
        Assert.Null(exception);
=======
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
>>>>>>> origin/dev
    }

    [Fact]
    public async Task UpdateApiKeyNameExistTest()
    {
<<<<<<< HEAD
        var apiKeyInfo = new ProjectAppIdInfo(_firstApikeyId, _projectId, _firstApiKeyName, _appSecretKey, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.GetAsync(_firstApikeyId)).ReturnsAsync(apiKeyInfo);
        _apiKeysRepository.Setup(expression: s => s.CheckProjectAppNameExist(_projectId, _secondApiKeyName))
            .ReturnsAsync(true);

        var projectApiKeyService = new ProjectAppIdService(_apiKeysRepository.Object, _logger.Object,
            _organizationPermissionChecker.Object, _userAppService.Object, _identityUserManager.Object);

        var exception = await Record.ExceptionAsync(async () =>
            await projectApiKeyService.ModifyApiKeyAsync(_firstApikeyId, _secondApiKeyName));
        Assert.NotNull(exception);
=======
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
>>>>>>> origin/dev
    }
}