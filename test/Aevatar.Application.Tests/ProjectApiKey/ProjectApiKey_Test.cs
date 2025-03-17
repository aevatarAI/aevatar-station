using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Aevatar.Common;
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
    private readonly IProjectApiKeyService _projectApiKeyService;
    private readonly Guid _projectId = Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703");
    private readonly Guid _currentUserId = Guid.Parse("ca63293c-fdde-4730-b10b-e95c37379702");
    private readonly Mock<IApiKeysRepository> _apiKeysRepository;
    private readonly Mock<IdentityUserManager> _identityUserManager;
    private readonly Mock<ILogger<ProjectApiKeyService>> _logger;
    private readonly Guid _firstApikeyId = Guid.Parse("df63293c-fdde-4730-b10b-e95c37379732");
    private readonly string _firstApiKeyName = "FirstApiKey";
    private readonly Guid _secondApikeyId = Guid.Parse("cd63293c-fdde-4730-b10b-e95c3737973d");
    private readonly string _secondApiKeyName = "SecondApiKey";
    private readonly CancellationToken _cancellationToken = new CancellationToken();
    private readonly ApiKeyInfo _apiKeyInfo;

    public ProjectApiKey_Test()
    {
        _apiKeysRepository = new Mock<IApiKeysRepository>();
        _identityUserManager = new Mock<IdentityUserManager>();
        _logger = new Mock<ILogger<ProjectApiKeyService>>();
    }

    [Fact]
    public async Task CreateApiKeyTest()
    {
        var apiKeyInfo = new ApiKeyInfo(_firstApikeyId, _projectId, _firstApiKeyName, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.InsertAsync(apiKeyInfo, false, _cancellationToken))
            .ReturnsAsync(apiKeyInfo);
        _apiKeysRepository.Setup(s => s.CheckProjectApiKeyNameExist(_projectId, _firstApiKeyName)).ReturnsAsync(false);

        _identityUserManager.Setup(s => s.GetByIdAsync(_currentUserId))
            .ReturnsAsync(new IdentityUser(_currentUserId, "A", "2222@gmail.com"));

        var projectApiKeyService = new ProjectApiKeyService(_apiKeysRepository.Object, _logger.Object);
        await projectApiKeyService.CreateAsync(_projectId, _firstApiKeyName, _currentUserId);
    }


    [Fact]
    public async Task CreateApiKeyExistTest()
    {
        var apiKeyInfo = new ApiKeyInfo(_secondApikeyId, _projectId, _secondApiKeyName, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.InsertAsync(apiKeyInfo, false, _cancellationToken))
            .ReturnsAsync(apiKeyInfo);
        
        _apiKeysRepository.Setup(s => s.CheckProjectApiKeyNameExist(_projectId, _firstApiKeyName)).ReturnsAsync(true);
        _identityUserManager.Setup(s => s.GetByIdAsync(_currentUserId))
            .ReturnsAsync(new IdentityUser(_currentUserId, "A", "2222@gmail.com"));

        var projectApiKeyService = new ProjectApiKeyService(_apiKeysRepository.Object, _logger.Object);

        await Assert.ThrowsAsync<BusinessException>(async () =>
            await projectApiKeyService.CreateAsync(_projectId, _firstApiKeyName, _currentUserId));
    }

    [Fact]
    public async Task UpdateApiKeyNameTest()
    {
        var apiKeyInfo = new ApiKeyInfo(_firstApikeyId, _projectId, _firstApiKeyName, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.GetAsync(_firstApikeyId)).ReturnsAsync(apiKeyInfo);
        var projectApiKeyService = new ProjectApiKeyService(_apiKeysRepository.Object, _logger.Object);

        var exception = await Record.ExceptionAsync(async () => await projectApiKeyService.ModifyApiKeyAsync(_firstApikeyId, "bbbb"));
        Assert.Null(exception);
    }
    
    [Fact]
    public async Task UpdateApiKeyNameExistTest()
    {
        var apiKeyInfo = new ApiKeyInfo(_firstApikeyId, _projectId, _firstApiKeyName, "aaaaa")
        {
            CreationTime = DateTime.Now,
            CreatorId = Guid.NewGuid(),
        };

        _apiKeysRepository.Setup(expression: s => s.GetAsync(_firstApikeyId)).ReturnsAsync(apiKeyInfo);
        _apiKeysRepository.Setup(expression: s => s.CheckProjectApiKeyNameExist(_projectId, _secondApiKeyName)).ReturnsAsync(true);
        
        var projectApiKeyService = new ProjectApiKeyService(_apiKeysRepository.Object, _logger.Object);

        var exception = await Record.ExceptionAsync(async () => await projectApiKeyService.ModifyApiKeyAsync(_firstApikeyId, _secondApiKeyName));
        Assert.NotNull(exception);
    }
}