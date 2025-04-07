using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.TokenUsage;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("TokenUsage")]
[Route("api/token-usage")]
[Authorize]
public class TokenUsageController: AevatarController
{
    private readonly ILogger<TokenUsageController> _logger;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IOrganizationPermissionChecker _organizationPermission;
    private readonly IOptions<SystemLLMConfigOptions> _options;

    public TokenUsageController(ITokenUsageService tokenUsageService, ILogger<TokenUsageController> logger, IOrganizationPermissionChecker organizationPermission, IOptions<SystemLLMConfigOptions> options)
    {
        _tokenUsageService = tokenUsageService;
        _logger = logger;
        _organizationPermission = organizationPermission;
        _options = options;
    }

    [HttpPost]
    public async Task<List<TokenUsageResponseDto>> GetTokenUsage(TokenUsageRequestDto requestDto)
    {
        // todo: modify to organization checker
        
        return await _tokenUsageService.GetTokenUsageAsync(requestDto);
    }

    
    [HttpGet("system-llm")]
    public async Task<List<string>> GetSystemLLM()
    {
        if (_options.Value.SystemLLMConfigs == null)
        {
            return new List<string>();
        }
        
        return _options.Value.SystemLLMConfigs.Select(s => s.Key).ToList();
    }
}