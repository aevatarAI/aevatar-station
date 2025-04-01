using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Aevatar.TokenUsage;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

    public TokenUsageController(ITokenUsageService tokenUsageService, ILogger<TokenUsageController> logger, IOrganizationPermissionChecker organizationPermission)
    {
        _tokenUsageService = tokenUsageService;
        _logger = logger;
        _organizationPermission = organizationPermission;
    }

    [HttpPost]
    public async Task<List<TokenUsageResponseDto>> GetTokenUsage(TokenUsageRequestDto requestDto)
    {
        // todo: modify to organization checker
        
        return await _tokenUsageService.GetTokenUsageAsync(requestDto);
    }
}