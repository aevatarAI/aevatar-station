using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Service;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using JsonException = Newtonsoft.Json.JsonException;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Aevatar.Application.Service;

/// <summary>
/// 统一的工作流编排服务实现（使用新的Agent描述获取方案）
/// </summary>
public class WorkflowOrchestrationService : IWorkflowOrchestrationService
{
    private readonly ILogger<WorkflowOrchestrationService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IUserAppService _userAppService;
    private readonly IGAgentManager _gAgentManager;
    private readonly IGAgentFactory _gAgentFactory;

    public WorkflowOrchestrationService(
        ILogger<WorkflowOrchestrationService> logger,
        IClusterClient clusterClient,
        IUserAppService userAppService,
        IGAgentManager gAgentManager,
        IGAgentFactory gAgentFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _userAppService = userAppService;
        _gAgentManager = gAgentManager;
        _gAgentFactory = gAgentFactory;
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

        var currentUserId = _userAppService.GetCurrentUserId();
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
                workflowConfig.Properties?.WorkflowNodeList?.Count ?? 0, 
                workflowConfig.Properties?.WorkflowNodeUnitList?.Count ?? 0);

            return workflowConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating workflow for user {UserId} with goal: {UserGoal}", currentUserId, userGoal);
            return null;
        }
    }

    #region Private Methods - Agent Description Acquisition

    /// <summary>
    /// 使用新方案获取所有可用Agent的丰富描述信息（从grain的GetDescriptionAsync获取JSON）
    /// </summary>
    private async Task<List<AgentDescriptionInfo>> GetAgentDescriptionsAsync()
    {
        try
        {
            _logger.LogDebug("Getting available agent types from GAgentManager");
            var availableTypes = _gAgentManager.GetAvailableGAgentTypes();
            var validAgentTypes = availableTypes.Where(t => !t.Namespace?.StartsWith("OrleansCodeGen") == true).ToList();
            
            _logger.LogInformation("Found {TypeCount} valid agent types to process", validAgentTypes.Count);
            
            var agentDescriptions = new List<AgentDescriptionInfo>();
            
            foreach (var agentType in validAgentTypes)
            {
                try
                {
                    _logger.LogDebug("Processing agent type: {AgentType}", agentType.FullName);
                    
                    // 为每个类型创建默认grain实例（遵循AgentService的模式）
                    var grainTypeName = agentType.FullName ?? agentType.Name;
                    var grainId = GrainId.Create(grainTypeName, 
                        GuidUtil.GuidToGrainKey(GuidUtil.StringToGuid("AgentDefaultId")));
                    
                    var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                    
                    // 调用新的JSON格式GetDescriptionAsync
                    var jsonDescription = await agent.GetDescriptionAsync();
                    
                    // 尝试反序列化为AgentDescriptionInfo
                    AgentDescriptionInfo? agentInfo = null;
                    try
                    {
                        agentInfo = JsonConvert.DeserializeObject<AgentDescriptionInfo>(jsonDescription);
                    }
                    catch (JsonException)
                    {
                        // 向后兼容：如果JSON反序列化失败，创建基本的AgentDescriptionInfo
                        _logger.LogDebug("Agent {AgentType} returned legacy text description, creating basic AgentDescriptionInfo", agentType.Name);
                        agentInfo = new AgentDescriptionInfo
                        {
                            Id = agentType.FullName ?? agentType.Name,
                            Name = agentType.Name,
                            L1Description = jsonDescription.Length > 150 ? jsonDescription.Substring(0, 147) + "..." : jsonDescription,
                            L2Description = jsonDescription,
                            Category = InferAgentCategory(agentType.Name),
                            Capabilities = new List<string> { "General processing" },
                            Tags = new List<string> { "agent", "processing" }
                        };
                    }
                    
                    if (agentInfo != null)
                    {
                        // 确保基本字段有值
                        if (string.IsNullOrEmpty(agentInfo.Name))
                            agentInfo.Name = agentType.Name;
                        if (string.IsNullOrEmpty(agentInfo.Id))
                            agentInfo.Id = agentType.FullName ?? agentType.Name;
                        
                        agentDescriptions.Add(agentInfo);
                        _logger.LogDebug("Successfully processed agent {AgentType} with description: {L1Description}", 
                            agentType.Name, agentInfo.L1Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get description for agent type {AgentType}", agentType.FullName);
                }
            }
            
            _logger.LogInformation("Successfully retrieved {AgentCount} agent descriptions using new JSON method", agentDescriptions.Count);
            return agentDescriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting agent descriptions");
            return new List<AgentDescriptionInfo>();
        }
    }

    /// <summary>
    /// 推断Agent类别（基于名称）
    /// </summary>
    private string InferAgentCategory(string agentName)
    {
        var name = agentName.ToLower();
        if (name.Contains("twitter") || name.Contains("telegram") || name.Contains("social"))
            return "Social";
        else if (name.Contains("ai") || name.Contains("chat") || name.Contains("llm"))
            return "AI";
        else if (name.Contains("blockchain") || name.Contains("aelf") || name.Contains("pump"))
            return "Blockchain";
        else if (name.Contains("workflow") || name.Contains("orchestr") || name.Contains("router"))
            return "Workflow";
        else
            return "General";
    }

    #endregion

    #region Private Methods - AI Agent Integration

    /// <summary>
    /// 调用WorkflowComposerGAgent生成工作流JSON（使用新的Agent描述信息）
    /// </summary>
    private async Task<string> CallWorkflowComposerGAgentAsync(string userGoal, Guid userId)
    {
        try
        {
            _logger.LogInformation("Creating new WorkflowComposerGAgent instance for user {UserId}", userId);
            
            // 1. 使用新方案获取所有可用的Agent描述信息（从grain的JSON格式描述）
            _logger.LogDebug("Fetching available agents information using new grain-based method");
            var availableAgents = await GetAgentDescriptionsAsync();
            _logger.LogInformation("Retrieved {AgentCount} available agents with rich descriptions for workflow generation", availableAgents.Count);
            
            // 记录Agent信息用于调试
            foreach (var agent in availableAgents)
            {
                if (agent.Capabilities?.Any() == true)
                {
                    _logger.LogDebug("Agent {AgentName} has {CapabilityCount} capabilities: {Capabilities}", 
                        agent.Name, agent.Capabilities.Count, 
                        string.Join(", ", agent.Capabilities));
                }
                
                if (!string.IsNullOrEmpty(agent.Category))
                {
                    _logger.LogDebug("Agent {AgentName} category: {Category}", 
                        agent.Name, agent.Category);
                }
            }
            
            // 2. 创建WorkflowComposerGAgent实例并传递丰富的描述信息
            var instanceId = $"workflow-composer-{userId}-{DateTimeOffset.UtcNow.Ticks}";
            var workflowComposerGAgent = _clusterClient.GetGrain<IWorkflowComposerGAgent>(instanceId);
            // 关键修复：AIGAgent需要先调用InitializeAsync()进行初始化
            await workflowComposerGAgent.InitializeAsync(new InitializeDto()
            {
                Instructions = "You are an expert workflow designer that creates sophisticated agent-based workflows. Generate well-structured JSON configurations for complex multi-agent workflows.",
                LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
            });
            
            var result = await workflowComposerGAgent.GenerateWorkflowJsonAsync(userGoal, availableAgents);
            
            _logger.LogInformation("WorkflowComposerGAgent instance {InstanceId} completed successfully for user {UserId}", instanceId, userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WorkflowComposerGAgent call for user {UserId}", userId);
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
    private async Task<WorkflowViewConfigDto?> ParseWorkflowJsonToViewConfigAsync(string jsonContent)
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
            
            // Parse to DTO using case-insensitive options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Try to deserialize as the new format first
            var workflowConfig = System.Text.Json.JsonSerializer.Deserialize<WorkflowViewConfigDto>(cleanJson, options);
            
            if (workflowConfig == null)
            {
                _logger.LogError("Failed to deserialize workflow JSON to WorkflowViewConfigDto");
                return null;
            }

            // Validate and fix required fields
            if (string.IsNullOrWhiteSpace(workflowConfig.Name))
            {
                _logger.LogWarning("Top-level workflow name is missing or empty");
                workflowConfig.Name = "Unnamed Workflow";
            }

            if (workflowConfig.Properties == null)
            {
                _logger.LogWarning("Properties object is missing, creating default");
                workflowConfig.Properties = new WorkflowPropertiesDto
                {
                    Name = workflowConfig.Name,
                    WorkflowNodeList = new List<WorkflowNodeDto>(),
                    WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>()
                };
            }

            // Ensure properties name matches top-level name
            if (string.IsNullOrWhiteSpace(workflowConfig.Properties.Name))
            {
                workflowConfig.Properties.Name = workflowConfig.Name;
            }

            if (workflowConfig.Properties.WorkflowNodeList == null || !workflowConfig.Properties.WorkflowNodeList.Any())
            {
                _logger.LogWarning("No workflow nodes found in the configuration");
                workflowConfig.Properties.WorkflowNodeList = new List<WorkflowNodeDto>();
            }

            if (workflowConfig.Properties.WorkflowNodeUnitList == null)
            {
                _logger.LogWarning("No workflow node units found, initializing empty list");
                workflowConfig.Properties.WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>();
            }

            // Validate node structure - remove hardcoded positioning
            foreach (var node in workflowConfig.Properties.WorkflowNodeList)
            {
                if (node.ExtendedData == null)
                {
                    node.ExtendedData = new WorkflowNodeExtendedDataDto();
                    _logger.LogDebug("Node {NodeId} will use frontend auto-layout based on connections", node.NodeId);
                }

                if (node.Properties == null)
                {
                    node.Properties = new Dictionary<string, object>();
                }
            }

            _logger.LogInformation("Successfully parsed workflow JSON to view config with {NodeCount} nodes and {ConnectionCount} connections", 
                workflowConfig.Properties.WorkflowNodeList.Count, workflowConfig.Properties.WorkflowNodeUnitList.Count);

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