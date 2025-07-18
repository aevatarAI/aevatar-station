using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Domain.WorkflowOrchestration;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Users;

namespace Aevatar.Application.Service;

/// <summary>
/// 统一的工作流编排服务实现（简化版：仅负责调用AI Agent和解析结果）
/// </summary>
public class WorkflowOrchestrationService : IWorkflowOrchestrationService
{
    private readonly ILogger<WorkflowOrchestrationService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ICurrentUser _currentUser;

    public WorkflowOrchestrationService(
        ILogger<WorkflowOrchestrationService> logger,
        IClusterClient clusterClient,
        ICurrentUser currentUser)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 根据用户目标生成工作流视图配置，直接返回前端可渲染的格式
    /// </summary>
    /// <param name="userGoal">用户目标描述</param>
    /// <returns>前端可渲染的工作流视图配置</returns>
    public async Task<WorkflowViewConfigDto?> GenerateWorkflowAsync(string userGoal)
    {
        if (string.IsNullOrWhiteSpace(userGoal))
        {
            _logger.LogWarning("Empty user goal provided for workflow generation");
            return null;
        }

        var currentUserId = _currentUser.Id ?? Guid.NewGuid();
        _logger.LogInformation("Starting workflow generation for user {UserId} with goal: {UserGoal}", currentUserId, userGoal);

        try
        {
            // 调用用户专属的WorkflowComposerGAgent处理所有AI相关逻辑
            var workflowJson = await CallWorkflowComposerGAgentAsync(userGoal, currentUserId);
            _logger.LogDebug("Received workflow JSON with length: {Length}", workflowJson.Length);

            // 解析为前端格式
            var workflowConfig = await ParseWorkflowJsonToViewConfigAsync(workflowJson);

            if (workflowConfig == null)
            {
                _logger.LogError("Failed to parse workflow JSON to view configuration for user {UserId}", currentUserId);
                return null;
            }

            _logger.LogInformation("Successfully generated workflow view configuration for user {UserId} with {NodeCount} nodes and {ConnectionCount} connections", 
                currentUserId,
                workflowConfig.WorkflowNodeList?.Count ?? 0, 
                workflowConfig.WorkflowNodeUnitList?.Count ?? 0);

            return workflowConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating workflow for user {UserId} with goal: {UserGoal}", currentUserId, userGoal);
            return null;
        }
    }

    #region Private Methods - AI Agent Integration

    /// <summary>
    /// 调用用户专属的WorkflowComposerGAgent生成工作流JSON（用户隔离）
    /// </summary>
    private async Task<string> CallWorkflowComposerGAgentAsync(string userGoal, Guid userId)
    {
        try
        {
            _logger.LogInformation("Calling user-specific WorkflowComposerGAgent for user {UserId}", userId);
            
            // 使用用户专属的grainId确保用户隔离
            var userSpecificGrainId = $"workflow-composer-{userId}";
            var workflowComposerGAgent = _clusterClient.GetGrain<IWorkflowComposerGAgent>(userSpecificGrainId);
            var result = await workflowComposerGAgent.GenerateWorkflowJsonAsync(userGoal);
            
            _logger.LogInformation("User-specific WorkflowComposerGAgent completed successfully for user {UserId}", userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user-specific WorkflowComposerGAgent call for user {UserId}", userId);
            throw; // 让上层处理异常
        }
    }

    #endregion

    #region Private Methods - JSON Parsing

    /// <summary>
    /// Parse workflow JSON to frontend format DTO
    /// </summary>
    /// <param name="jsonContent">JSON content from WorkflowComposerGAgent</param>
    /// <returns>Parsed WorkflowViewConfigDto</returns>
    public async Task<WorkflowViewConfigDto?> ParseWorkflowJsonToViewConfigAsync(string jsonContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty JSON content provided for parsing");
                return null;
            }

            // Clean and validate JSON
            var cleanJson = CleanJsonContent(jsonContent);
            
            // Parse to DTO
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var workflowConfig = JsonSerializer.Deserialize<WorkflowViewConfigDto>(cleanJson, options);
            
            if (workflowConfig == null)
            {
                _logger.LogError("Failed to deserialize workflow JSON to WorkflowViewConfigDto");
                return null;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(workflowConfig.Name))
            {
                _logger.LogWarning("Workflow name is missing or empty");
                workflowConfig.Name = "Unnamed Workflow";
            }

            if (workflowConfig.WorkflowNodeList == null || !workflowConfig.WorkflowNodeList.Any())
            {
                _logger.LogWarning("No workflow nodes found in the configuration");
                workflowConfig.WorkflowNodeList = new List<WorkflowNodeDto>();
            }

            if (workflowConfig.WorkflowNodeUnitList == null)
            {
                _logger.LogWarning("No workflow node units found, initializing empty list");
                workflowConfig.WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>();
            }

            _logger.LogInformation("Successfully parsed workflow JSON to view config with {NodeCount} nodes and {ConnectionCount} connections", 
                workflowConfig.WorkflowNodeList.Count, workflowConfig.WorkflowNodeUnitList.Count);

            return workflowConfig;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error when converting to WorkflowViewConfigDto");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when parsing workflow JSON to view config");
            return null;
        }
    }

    /// <summary>
    /// 清理JSON内容（移除markdown标记等）
    /// </summary>
    private string CleanJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return string.Empty;

        var cleaned = jsonContent.Trim();

        // 移除markdown代码块标记
        if (cleaned.StartsWith("```json"))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }

        return cleaned.Trim();
    }

    #endregion
} 