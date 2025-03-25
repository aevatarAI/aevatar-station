using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Aevatar.ApiKeys;
using Aevatar.APIKeys;
using Aevatar.Common;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.Service;

public class ProjectApiKeyService : IProjectApiKeyService, ITransientDependency
{
    private readonly IApiKeysRepository _apiKeysRepository;
    private readonly ILogger<ProjectApiKeyService> _logger;
    private readonly IOrganizationPermissionChecker _organizationPermission;

    public ProjectApiKeyService(IApiKeysRepository apiKeysRepository, ILogger<ProjectApiKeyService> logger, IOrganizationPermissionChecker organizationPermission)
    {
        _apiKeysRepository = apiKeysRepository;
        _logger = logger;
        _organizationPermission = organizationPermission;
    }


    public async Task CreateAsync(Guid projectId, string keyName, Guid? currentUserId)
    {
        _logger.LogDebug($"[ProjectApiKeyService][CreateAsync] projectId:{projectId}, keyName:{keyName}");

        if (await _apiKeysRepository.CheckProjectApiKeyNameExist(projectId, keyName))
        {
            throw new BusinessException(message: "key name has exist");
        }

        var apikeyStr = MD5Util.CalculateMD5($"{projectId.ToString()}-{keyName}-{Guid.NewGuid()}");
        var apiKey = new ApiKeyInfo(Guid.NewGuid(), projectId, keyName, apikeyStr)
        {
            CreationTime = DateTime.Now,
            CreatorId = currentUserId,
        };

       await _apiKeysRepository.InsertAsync(apiKey);
    }

    public async Task DeleteAsync(Guid apiKeyId)
    {
        var apikeyInfo = await _apiKeysRepository.GetAsync(apiKeyId);
        if (apikeyInfo == null)
        {
            throw new UserFriendlyException("Api key not found");
        }
        
        await _organizationPermission.AuthenticateAsync(apikeyInfo.ProjectId, AevatarPermissions.ApiKeys.Delete);
        _logger.LogDebug($"[ProjectApiKeyService][DeleteAsync] apiKeyId:{apiKeyId}");
        await _apiKeysRepository.HardDeleteAsync(f => f.Id == apiKeyId);
    }

    public async Task ModifyApiKeyAsync(Guid apiKeyId, string keyName)
    {
        _logger.LogDebug($"[ProjectApiKeyService][ModifyApiKeyAsync] apiKeyId:{apiKeyId}, keyName:{keyName}");

        var apiKeyInfo = await _apiKeysRepository.GetAsync(apiKeyId);
        if (apiKeyInfo == null)
        {
            throw new BusinessException(message: "ApiKey not exist");
        }

        await _organizationPermission.AuthenticateAsync(apiKeyInfo.ProjectId, AevatarPermissions.ApiKeys.Edit);
        if (await _apiKeysRepository.CheckProjectApiKeyNameExist(apiKeyInfo.ProjectId, keyName))
        {
            throw new BusinessException(message: "key name has exist");
        }

        if (apiKeyInfo.ApiKeyName == keyName)
        {
            throw new BusinessException(message: "ApiKey is the same ");
        }


        apiKeyInfo.ApiKeyName = keyName;

        await _apiKeysRepository.UpdateAsync(apiKeyInfo);
    }

    public async Task<List<ApiKeyInfo>> GetApiKeysAsync(Guid projectId)
    {
        APIKeyPagedRequestDto requestDto = new APIKeyPagedRequestDto()
            { ProjectId = projectId, MaxResultCount = 10, SkipCount = 0 };

        var apiKeyList = await _apiKeysRepository.GetProjectApiKeys(requestDto);
        return apiKeyList.Items.ToList();
    }
}