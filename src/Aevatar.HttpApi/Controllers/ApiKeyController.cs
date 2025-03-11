using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.ApiKeys;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrleansCodeGen.Orleans.EventSourcing.LogStorage;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Agent")]
[Microsoft.AspNetCore.Components.Route("api/apikey")]
[Authorize]
public class ApiKeyController : AevatarController
{
    private readonly IProjectApiKeyService _apiKeyService;

    public ApiKeyController(IProjectApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }


    [HttpPost]
    public async Task CreateApiKey(CreateApiKeyDto createDto)
    {
        await _apiKeyService.CreateAsync(createDto.ProjectId, createDto.KeyName);
    }

    [HttpGet("{guid}")]
    public async Task<List<ApiKeyListResponseDto>> GetApiKeys(Guid guid)
    {
        return await _apiKeyService.GetApiKeysAsync(guid);
    }

    [HttpDelete("{guid}")]
    public async Task DeleteApiKey(Guid guid)
    {
        await _apiKeyService.DeleteAsync(guid);
    }

    [HttpPut("{guid}")]
    public async Task ModifyApiKeyName(Guid guid, [FromBody] ModifyApiKeyNameDto modifyApiKeyNameDto)
    {
        await _apiKeyService.ModifyApiKeyAsync(guid, modifyApiKeyNameDto.ApiKeyName);
    }
}