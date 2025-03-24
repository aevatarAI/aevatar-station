using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.ApiKeys;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrleansCodeGen.Orleans.EventSourcing.LogStorage;
using Volo.Abp;
using Volo.Abp.Identity;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("ApiKey")]
[Route("api/apikey")]
[Authorize]
public class ApiKeyController : AevatarController
{
    private readonly IProjectApiKeyService _apiKeyService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IOrganizationPermissionChecker _organizationPermission;

    public ApiKeyController(IProjectApiKeyService apiKeyService, IdentityUserManager identityUserManager, IOrganizationPermissionChecker organizationPermission)
    {
        _apiKeyService = apiKeyService;
        _identityUserManager = identityUserManager;
        _organizationPermission = organizationPermission;
    }


    [HttpPost]
    public async Task CreateApiKey(CreateApiKeyDto createDto)
    {
        await _organizationPermission.AuthenticateAsync(createDto.ProjectId, AevatarPermissions.ApiKeys.Create);
        await _apiKeyService.CreateAsync(createDto.ProjectId, createDto.KeyName, CurrentUser.Id);
    }

    [HttpGet("{guid}")]
    public async Task<List<ApiKeyListResponseDto>> GetApiKeys(Guid guid)
    {
        await _organizationPermission.AuthenticateAsync(guid, AevatarPermissions.ApiKeys.Default);
        var result = new List<ApiKeyListResponseDto>();
        foreach (var item in await _apiKeyService.GetApiKeysAsync(guid))
        {
            var creatorInfo = await _identityUserManager.GetByIdAsync((Guid)item.CreatorId!);
            result.Add(new ApiKeyListResponseDto()
            {
                Id = item.Id,
                ApiKey = item.ApiKey,
                ApiKeyName = item.ApiKeyName,
                CreateTime = item.CreationTime,
                CreatorName = creatorInfo.Name,
                ProjectId = item.ProjectId,
            });
        }

        return result;
    }

    [HttpDelete("{guid}")]
    public async Task DeleteApiKey(Guid guid)
    {
        await _organizationPermission.AuthenticateAsync(guid, AevatarPermissions.ApiKeys.Delete);
        await _apiKeyService.DeleteAsync(guid);
    }

    [HttpPut("{guid}")]
    public async Task ModifyApiKeyName(Guid guid, [FromBody] ModifyApiKeyNameDto modifyApiKeyNameDto)
    {
        await _organizationPermission.AuthenticateAsync(guid, AevatarPermissions.ApiKeys.Edit);
        await _apiKeyService.ModifyApiKeyAsync(guid, modifyApiKeyNameDto.ApiKeyName);
    }
}