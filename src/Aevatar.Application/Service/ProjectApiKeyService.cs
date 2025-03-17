using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Aevatar.ApiKeys;
using Aevatar.Common;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Aevatar.Service;

public interface IProjectApiKeyService
{
    Task CreateAsync(Guid projectId, string keyName, Guid? currentUserId);
    Task DeleteAsync(Guid apiKeyId);
    Task ModifyApiKeyAsync(Guid apiKeyId, string keyName);
    Task<List<ApiKeyInfo>> GetApiKeysAsync(Guid projectId);
}

public class ProjectApiKeyService : IProjectApiKeyService, ITransientDependency
{
    private readonly IApiKeysRepository _apiKeysRepository;
    private readonly ILogger<ProjectApiKeyService> _logger;

    public ProjectApiKeyService(IApiKeysRepository apiKeysRepository, ILogger<ProjectApiKeyService> logger)
    {
        _apiKeysRepository = apiKeysRepository;
        _logger = logger;
    }


    public async Task CreateAsync(Guid projectId, string keyName, Guid? currentUserId)
    {
        // todo:validate create rights

        _logger.LogDebug($"[ProjectApiKeyService][CreateAsync] projectId:{projectId}, keyName:{keyName}");

        if (await _apiKeysRepository.CheckProjectApiKeyNameExist(projectId, keyName))
        {
            throw new BusinessException(message: "key name has exist");
        }

        var random = new Random();
        var randNum = random.Next(0, 1000000000);
        var apikeyStr = MD5Util.CalculateMD5($"{projectId.ToString()}-{keyName}-{randNum}");

        var apiKey = new ApiKeyInfo(Guid.NewGuid(), projectId, keyName, apikeyStr)
        {
            CreationTime = DateTime.Now,
            CreatorId = currentUserId,
        };

       await _apiKeysRepository.InsertAsync(apiKey);
    }

    public async Task DeleteAsync(Guid apiKeyId)
    {
        // todo:validate delete rights
        _logger.LogDebug($"[ProjectApiKeyService][DeleteAsync] apiKeyId:{apiKeyId}");
        await _apiKeysRepository.HardDeleteAsync(f => f.Id == apiKeyId);
    }

    public async Task ModifyApiKeyAsync(Guid apiKeyId, string keyName)
    {
        // todo:validate modify rights
        _logger.LogDebug($"[ProjectApiKeyService][ModifyApiKeyAsync] apiKeyId:{apiKeyId}, keyName:{keyName}");

        var apiKeyInfo = await _apiKeysRepository.GetAsync(apiKeyId);
        if (apiKeyInfo == null)
        {
            throw new BusinessException(message: "ApiKey not exist");
        }

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
        // todo:validate GetApiKeysAsync rights

        var apiKeyList = await _apiKeysRepository.GetProjectApiKeys(projectId, 10, 0);
       
        return apiKeyList;
    }
}