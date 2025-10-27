using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("AgentConfig")]
[Route("api/admin/agent-config")]
public class AgentConfigController : AevatarController
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AgentConfigController> _logger;

    public AgentConfigController(IClusterClient clusterClient, ILogger<AgentConfigController> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    /// <summary>
    /// Batch reload agent LLM configuration from appsettings.json
    /// </summary>
    /// <param name="request">Request containing user IDs to reload</param>
    /// <returns>Batch operation result</returns>
    [HttpPost("batch-reload")]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    public async Task<BatchReloadResultDto> BatchReloadAsync([FromBody] BatchReloadRequestDto request)
    {
        if (request == null || request.UserIds == null || !request.UserIds.Any())
        {
            throw new UserFriendlyException("UserIds cannot be empty");
        }

        _logger.LogInformation("BatchReloadAsync: Starting batch reload for {Count} users", request.UserIds.Count);

        var results = new List<ReloadResultDto>();
        var successCount = 0;
        var failCount = 0;

        foreach (var userIdStr in request.UserIds)
        {
            try
            {
                if (!Guid.TryParse(userIdStr, out var userId))
                {
                    _logger.LogWarning("Invalid userId format: {UserId}", userIdStr);
                    results.Add(new ReloadResultDto
                    {
                        UserId = userIdStr,
                        Success = false,
                        Message = "Invalid userId format"
                    });
                    failCount++;
                    continue;
                }

                var aiAgent = _clusterClient.GetGrain<IAIGAgent>(userId);
                
                // Force reload config by calling InitializeAsync with current settings
                var stateGAgent = aiAgent as IStateGAgent<AIGAgentStateBase>;
                if (stateGAgent == null)
                {
                    _logger.LogWarning("Agent is not IStateGAgent: {UserId}", userId);
                    results.Add(new ReloadResultDto
                    {
                        UserId = userIdStr,
                        Success = false,
                        Message = "Agent does not support state management"
                    });
                    failCount++;
                    continue;
                }

                var state = await stateGAgent.GetStateAsync();
                if (state == null || string.IsNullOrEmpty(state.SystemLLM))
                {
                    _logger.LogWarning("Agent state is empty or SystemLLM is not set: {UserId}", userId);
                    results.Add(new ReloadResultDto
                    {
                        UserId = userIdStr,
                        Success = false,
                        Message = "Agent state is empty or SystemLLM is not set"
                    });
                    failCount++;
                    continue;
                }

                // Call InitializeAsync to reload config
                var reloadResult = await aiAgent.InitializeAsync(new InitializeDto
                {
                    Instructions = string.IsNullOrEmpty(state.PromptTemplate) ? "You are an AI agent." : state.PromptTemplate,
                    LLMConfig = new LLMConfigDto
                    {
                        SystemLLM = state.SystemLLM
                    }
                });

                if (reloadResult)
                {
                    _logger.LogInformation("Successfully reloaded config for user: {UserId}, SystemLLM: {SystemLLM}", 
                        userId, state.SystemLLM);
                    results.Add(new ReloadResultDto
                    {
                        UserId = userIdStr,
                        Success = true,
                        Message = $"Config reloaded successfully for {state.SystemLLM}"
                    });
                    successCount++;
                }
                else
                {
                    _logger.LogWarning("Failed to reload config for user: {UserId}", userId);
                    results.Add(new ReloadResultDto
                    {
                        UserId = userIdStr,
                        Success = false,
                        Message = "InitializeAsync returned false, check logs for details"
                    });
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading config for user: {UserId}", userIdStr);
                results.Add(new ReloadResultDto
                {
                    UserId = userIdStr,
                    Success = false,
                    Message = $"Exception: {ex.Message}"
                });
                failCount++;
            }
        }

        _logger.LogInformation(
            "BatchReloadAsync: Batch reload completed. Success: {SuccessCount}, Failed: {FailCount}, Total: {Total}",
            successCount, failCount, request.UserIds.Count);

        return new BatchReloadResultDto
        {
            Success = true,
            Message = $"Batch reload completed. Success: {successCount}, Failed: {failCount}",
            TotalCount = request.UserIds.Count,
            SuccessCount = successCount,
            FailCount = failCount,
            Results = results
        };
    }
}

public class BatchReloadRequestDto
{
    public List<string> UserIds { get; set; }
}

public class BatchReloadResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<ReloadResultDto> Results { get; set; }
}

public class ReloadResultDto
{
    public string UserId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}

