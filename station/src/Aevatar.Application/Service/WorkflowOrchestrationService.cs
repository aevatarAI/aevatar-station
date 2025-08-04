using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
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
using Newtonsoft.Json.Linq;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Options;

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
    private readonly WorkflowOrchestrationPromptOptions _promptOptions;

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
        _promptOptions = new WorkflowOrchestrationPromptOptions(); // 使用默认配置
    }

    /// <summary>
    /// 根据用户目标生成工作流视图配置，直接返回前端可渲染的格式
    /// </summary>
    /// <param name="userGoal">用户目标描述</param>
    /// <returns>前端可渲染的工作流视图配置</returns>
    public async Task<AiWorkflowViewConfigDto?> GenerateWorkflowAsync(string userGoal)
    {
        if (string.IsNullOrWhiteSpace(userGoal))
        {
            _logger.LogWarning("Empty user goal provided for workflow generation");
            return null;
        }

        var currentUserId = _userAppService.GetCurrentUserId();
        
        using var scope = _logger.BeginScope("UserId: {UserId}, UserGoal: {UserGoal}", currentUserId, userGoal);
        _logger.LogInformation("Starting workflow generation with user goal: {UserGoal}", userGoal);

        try
        {
            // 调用用户专属的WorkflowComposerGAgent处理所有AI相关逻辑
            var workflowJson = await CallWorkflowComposerGAgentAsync(userGoal, currentUserId);
            _logger.LogDebug("Received workflow JSON with length: {Length}", workflowJson.Length);

            // 解析为前端格式
            var workflowConfig = await ParseWorkflowJsonToViewConfigAsync(workflowJson);

            if (workflowConfig == null)
            {
                _logger.LogError("Failed to parse workflow JSON to view configuration");
                return null;
            }

            _logger.LogInformation("Successfully generated workflow view configuration: {@WorkflowSummary}", 
                new { 
                    Name = workflowConfig.Name,
                    NodeCount = workflowConfig.Properties?.WorkflowNodeList?.Count ?? 0, 
                    ConnectionCount = workflowConfig.Properties?.WorkflowNodeUnitList?.Count ?? 0 
                });

            return workflowConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating workflow");
            return null;
        }
    }

    #region Private Methods - Agent Description Acquisition

    /// <summary>
    /// 使用反射+DescriptionAttribute方式获取所有可用Agent的描述信息
    /// 简化版本：只获取className和attribute中的description
    /// </summary>
    private async Task<List<AiWorkflowAgentInfoDto>> GetAgentDescriptionsAsync()
    {
        using var scope = _logger.BeginScope("Operation: GetAgentDescriptions");
        
        try
        {
            _logger.LogDebug("Getting available agent types from GAgentManager using reflection approach");
            var availableTypes = _gAgentManager.GetAvailableGAgentTypes();
            var validAgentTypes = availableTypes.Where(t => !t.Namespace?.StartsWith("OrleansCodeGen") == true).ToList();
            
            _logger.LogInformation("Found {TypeCount} valid agent types to process", validAgentTypes.Count);
            
            var agentDescriptions = new List<AiWorkflowAgentInfoDto>();
            
            foreach (var agentType in validAgentTypes)
            {
                try
                {
                    var agentInfo = CreateAgentInfo(agentType);
                    agentDescriptions.Add(agentInfo);
                    
                    _logger.LogDebug("Created agent info: {@AgentInfo}", 
                        new { agentInfo.Name, agentInfo.Type, agentInfo.Description });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process agent type {AgentType}", agentType.Name);
                }
            }
            
            _logger.LogInformation("Successfully retrieved {AgentCount} agent descriptions using reflection approach", agentDescriptions.Count);
            
            return agentDescriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting agent descriptions");
            return new List<AiWorkflowAgentInfoDto>();
        }
    }

    /// <summary>
    /// 根据Agent类型创建Agent信息DTO
    /// </summary>
    private AiWorkflowAgentInfoDto CreateAgentInfo(Type agentType)
    {
        var descriptionAttr = agentType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? $"{agentType.Name} - Agent for specialized processing";
        
        return new AiWorkflowAgentInfoDto
        {
            Name = agentType.Name,
            Type = agentType.FullName ?? agentType.Name,
            Description = description
        };
    }

    #endregion

    #region Private Methods - Prompt Building

    /// <summary>
    /// 构建配置化的系统提示词，用于WorkflowComposerGAgent初始化
    /// </summary>
    private string BuildSystemInstructionsPrompt(string userGoal, List<AiWorkflowAgentInfoDto> availableAgents)
    {
        try
        {
            _logger.LogDebug("Building system instructions prompt with {AgentCount} agents for goal: {UserGoal}", 
                availableAgents.Count, userGoal);

            var promptBuilder = new StringBuilder();

            // 1. 系统角色定义
            promptBuilder.AppendLine(_promptOptions.SystemRoleTemplate);
            promptBuilder.AppendLine();

            // 2. 用户目标部分
            var userGoalSection = _promptOptions.UserGoalSectionTemplate.Replace("{USER_GOAL}", userGoal);
            promptBuilder.AppendLine(userGoalSection);
            promptBuilder.AppendLine();

            // 3. 构建Agent目录内容
            var agentCatalogContent = BuildAgentCatalogContent(availableAgents);
            var agentCatalogSection = _promptOptions.AgentCatalogSectionTemplate.Replace("{AGENT_CATALOG_CONTENT}", agentCatalogContent);
            promptBuilder.AppendLine(agentCatalogSection);
            promptBuilder.AppendLine();

            // 4. 输出要求
            promptBuilder.AppendLine(_promptOptions.OutputRequirementsTemplate);
            promptBuilder.AppendLine();

            // 5. JSON格式规范
            promptBuilder.AppendLine(_promptOptions.JsonFormatSpecificationTemplate);

            var finalPrompt = promptBuilder.ToString();
            _logger.LogDebug("Built system instructions prompt with length: {PromptLength}", finalPrompt.Length);

            return finalPrompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building system instructions prompt");
            // 返回基础的提示词作为后备
            return _promptOptions.SystemRoleTemplate + "\n\n" + _promptOptions.JsonFormatSpecificationTemplate;
        }
    }

    /// <summary>
    /// 构建Agent目录内容
    /// </summary>
    private string BuildAgentCatalogContent(List<AiWorkflowAgentInfoDto> availableAgents)
    {
        if (!availableAgents.Any())
        {
            return _promptOptions.NoAgentsAvailableMessage;
        }

        var catalogBuilder = new StringBuilder();

        foreach (var agent in availableAgents)
        {
            var agentSection = _promptOptions.SingleAgentTemplate
                .Replace("{AGENT_NAME}", agent.Name)
                .Replace("{AGENT_TYPE}", agent.Type)
                .Replace("{AGENT_DESCRIPTION}", agent.Description);

            catalogBuilder.AppendLine(agentSection);
            catalogBuilder.AppendLine();
        }

        return catalogBuilder.ToString().TrimEnd();
    }

    #endregion

    #region Private Methods - AI Agent Integration

    /// <summary>
    /// 调用WorkflowComposerGAgent生成工作流JSON（使用新的Agent描述信息）
    /// </summary>
    private async Task<string> CallWorkflowComposerGAgentAsync(string userGoal, Guid userId)
    {
        using var scope = _logger.BeginScope("Operation: CallWorkflowComposerGAgent, UserId: {UserId}", userId);
        
        try
        {
            _logger.LogInformation("Creating new WorkflowComposerGAgent instance");
            
            // 1. 使用新方案获取所有可用的Agent描述信息（从grain的JSON格式描述）
            _logger.LogDebug("Fetching available agents information using new grain-based method");
            var availableAgents = await GetAgentDescriptionsAsync();
            _logger.LogInformation("Retrieved {AgentCount} available agents with rich descriptions for workflow generation", availableAgents.Count);
            
            // 记录Agent信息的汇总统计
            var agentSummary = new {
                TotalAgents = availableAgents.Count,
                AgentsWithDescription = availableAgents.Where(a => !string.IsNullOrEmpty(a.Description)).Count(),
                AgentNames = availableAgents.Select(a => a.Name).ToList()
            };
            _logger.LogDebug("Available agents summary for workflow generation: {@AgentSummary}", agentSummary);
            
            // 记录具体的Agent能力信息
            foreach (var agent in availableAgents.Where(a => !string.IsNullOrEmpty(a.Description)))
            {
                _logger.LogDebug("Agent info: {@AgentInfo}", 
                    new { agent.Name, agent.Type, agent.Description });
            }
            
            // 2. 创建WorkflowComposerGAgent实例并传递丰富的描述信息，使用GUID主键
            var instanceGuid = Guid.NewGuid(); // 使用GUID而不是字符串
            
            using var agentScope = _logger.BeginScope("WorkflowComposerGAgent: {InstanceGuid}", instanceGuid);
            _logger.LogDebug("Creating WorkflowComposerGAgent instance");
            
            var workflowComposerGAgent = _clusterClient.GetGrain<IWorkflowComposerGAgent>(instanceGuid);
            
            // 关键修复：AIGAgent需要先调用InitializeAsync()进行初始化
            // 使用配置化的系统提示词，包含用户目标和Agent信息
            var systemInstructions = BuildSystemInstructionsPrompt(userGoal, availableAgents);
            var initializeDto = new InitializeDto()
            {
                Instructions = systemInstructions,
                LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
            };
            _logger.LogDebug("Initializing WorkflowComposerGAgent with config: {@InitializeConfig}", 
                new { 
                    InstructionsLength = initializeDto.Instructions.Length,
                    SystemLLM = initializeDto.LLMConfig.SystemLLM,
                    AgentCount = availableAgents.Count
                });
            
            await workflowComposerGAgent.InitializeAsync(initializeDto);
            
            _logger.LogInformation("Generating workflow JSON with user goal: {UserGoal}", userGoal);
            var result = await workflowComposerGAgent.GenerateWorkflowJsonAsync(userGoal, availableAgents);
            
            _logger.LogInformation("WorkflowComposerGAgent completed successfully, result length: {ResultLength}", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WorkflowComposerGAgent call");
            throw; // 让上层处理异常
        }
    }

    #endregion

    #region Private Methods - JSON Parsing

    /// <summary>
    /// Parse workflow JSON to frontend format DTO
    /// </summary>
    /// <param name="jsonContent">JSON content from WorkflowComposerGAgent</param>
    /// <returns>Parsed AiWorkflowViewConfigDto</returns>
    private async Task<AiWorkflowViewConfigDto?> ParseWorkflowJsonToViewConfigAsync(string jsonContent)
    {
        try
        {
            _logger.LogInformation("开始解析工作流JSON. Content length: {ContentLength}", jsonContent?.Length ?? 0);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty JSON content provided for parsing");
                return null;
            }

            // Clean and validate JSON
            var cleanJson = CleanJsonContent(jsonContent);
            
            _logger.LogDebug("Parsing workflow JSON content: {CleanJson}", cleanJson);
            
            // Parse as JObject first to handle field mapping
            var jsonObject = JObject.Parse(cleanJson);
            
            // Create the mapped workflow configuration
            var workflowConfig = new AiWorkflowViewConfigDto();
            
            // Map top-level fields
            workflowConfig.Name = jsonObject["name"]?.ToString() ?? "Unnamed Workflow";
            
            // Handle properties object
            var propertiesObj = jsonObject["properties"] as JObject;
            if (propertiesObj != null)
            {
                workflowConfig.Properties = new AiWorkflowPropertiesDto
                {
                    Name = propertiesObj["name"]?.ToString() ?? workflowConfig.Name,
                    WorkflowNodeList = new List<AiWorkflowNodeDto>(),
                    WorkflowNodeUnitList = new List<AiWorkflowNodeUnitDto>()
                };
                
                // Map workflow nodes with field transformation
                var nodeListArray = propertiesObj["workflowNodeList"] as JArray;
                if (nodeListArray != null)
                {
                    foreach (var nodeToken in nodeListArray)
                    {
                        var nodeObj = nodeToken as JObject;
                        if (nodeObj != null)
                        {
                            var mappedNode = new AiWorkflowNodeDto
                            {
                                NodeId = nodeObj["nodeId"]?.ToString() ?? Guid.NewGuid().ToString(),
                                // Map AI's nodeType to frontend's agentType
                                AgentType = nodeObj["nodeType"]?.ToString() ?? nodeObj["agentType"]?.ToString() ?? "",
                                // Map AI's nodeName to frontend's name
                                Name = nodeObj["nodeName"]?.ToString() ?? nodeObj["name"]?.ToString() ?? "",
                                Properties = new Dictionary<string, object>()
                            };
                            
                            // Handle extended data mapping
                            var extendedDataObj = nodeObj["extendedData"] as JObject;
                            if (extendedDataObj != null)
                            {
                                mappedNode.ExtendedData = new AiWorkflowNodeExtendedDataDto
                                {
                                    // Use AI's position if provided, otherwise default to "0"
                                    XPosition = extendedDataObj["xPosition"]?.ToString() ?? "0",
                                    YPosition = extendedDataObj["yPosition"]?.ToString() ?? "0"
                                };
                                
                                // Store AI's description in properties for reference
                                var description = extendedDataObj["description"]?.ToString();
                                if (!string.IsNullOrEmpty(description))
                                {
                                    mappedNode.Properties["description"] = description;
                                }
                            }
                            else
                            {
                                mappedNode.ExtendedData = new AiWorkflowNodeExtendedDataDto
                                {
                                    XPosition = "0",
                                    YPosition = "0"
                                };
                            }
                            
                            // Copy node properties
                            var propertiesObj2 = nodeObj["properties"] as JObject;
                            if (propertiesObj2 != null)
                            {
                                foreach (var prop in propertiesObj2)
                                {
                                    mappedNode.Properties[prop.Key] = prop.Value?.ToObject<object>() ?? "";
                                }
                            }
                            
                            workflowConfig.Properties.WorkflowNodeList.Add(mappedNode);
                            _logger.LogDebug("Mapped node: {NodeId} -> AgentType: {AgentType}, Name: {Name}",
                                mappedNode.NodeId, mappedNode.AgentType, mappedNode.Name);
                        }
                    }
                }
                
                // Map workflow node connections with field transformation
                var nodeUnitArray = propertiesObj["workflowNodeUnitList"] as JArray;
                if (nodeUnitArray != null)
                {
                    foreach (var unitToken in nodeUnitArray)
                    {
                        var unitObj = unitToken as JObject;
                        if (unitObj != null)
                        {
                            var mappedUnit = new AiWorkflowNodeUnitDto
                            {
                                // Map AI's fromNodeId to frontend's nodeId
                                NodeId = unitObj["fromNodeId"]?.ToString() ?? unitObj["nodeId"]?.ToString() ?? "",
                                // Map AI's toNodeId to frontend's nextNodeId
                                NextNodeId = unitObj["toNodeId"]?.ToString() ?? unitObj["nextNodeId"]?.ToString() ?? ""
                            };
                            
                            workflowConfig.Properties.WorkflowNodeUnitList.Add(mappedUnit);
                            _logger.LogDebug("Mapped connection: {NodeId} -> {NextNodeId}", 
                                mappedUnit.NodeId, mappedUnit.NextNodeId);
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Properties object is missing, creating default");
                workflowConfig.Properties = new AiWorkflowPropertiesDto
                {
                    Name = workflowConfig.Name,
                    WorkflowNodeList = new List<AiWorkflowNodeDto>(),
                    WorkflowNodeUnitList = new List<AiWorkflowNodeUnitDto>()
                };
            }

            _logger.LogInformation("Successfully parsed and mapped workflow JSON to view config with {NodeCount} nodes and {ConnectionCount} connections",
                workflowConfig.Properties.WorkflowNodeList.Count, workflowConfig.Properties.WorkflowNodeUnitList.Count);

            // Apply intelligent layout algorithm after parsing
            ApplyIntelligentLayout(workflowConfig.Properties);

            return workflowConfig;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error when converting to AiWorkflowViewConfigDto. Content: {JsonContent}",
                jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when parsing workflow JSON to view config. Content: {JsonContent}",
                jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent);
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

    /// <summary>
    /// 应用智能布局算法，根据节点连接关系自动计算坐标
    /// </summary>
    private void ApplyIntelligentLayout(AiWorkflowPropertiesDto properties)
    {
        try
        {
            if (properties.WorkflowNodeList == null || properties.WorkflowNodeList.Count == 0)
            {
                _logger.LogWarning("No nodes to layout");
                return;
            }

            _logger.LogDebug("Applying intelligent layout to {NodeCount} nodes with {ConnectionCount} connections",
                properties.WorkflowNodeList.Count, properties.WorkflowNodeUnitList?.Count ?? 0);

            // 布局配置 - 使用高精度浮点数
            const double nodeWidth = 150.0;
            const double nodeHeight = 80.0;
            const double horizontalSpacing = 200.0;
            const double verticalSpacing = 120.0;
            const double startX = 100.0;
            const double startY = 100.0;

            // 构建节点连接关系图
            var nodeConnections = BuildNodeConnectionGraph(properties);

            // 计算节点层级
            var nodeLayers = CalculateNodeLayers(properties.WorkflowNodeList, nodeConnections);

            // 应用高精度层次布局
            ApplyHighPrecisionLayerLayout(properties.WorkflowNodeList, nodeLayers, 
                startX, startY, horizontalSpacing, verticalSpacing, nodeWidth, nodeHeight);

            _logger.LogInformation("Successfully applied intelligent layout. Nodes arranged in {LayerCount} layers",
                nodeLayers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying intelligent layout, falling back to simple grid layout");
            ApplyHighPrecisionGridLayout(properties.WorkflowNodeList);
        }
    }

    /// <summary>
    /// 构建节点连接关系图
    /// </summary>
    private Dictionary<string, List<string>> BuildNodeConnectionGraph(AiWorkflowPropertiesDto properties)
    {
        var connections = new Dictionary<string, List<string>>();
        
        // 初始化所有节点
        foreach (var node in properties.WorkflowNodeList)
        {
            connections[node.NodeId] = new List<string>();
        }

        // 添加连接关系
        foreach (var connection in properties.WorkflowNodeUnitList ?? new List<AiWorkflowNodeUnitDto>())
        {
            if (!string.IsNullOrEmpty(connection.NodeId) && !string.IsNullOrEmpty(connection.NextNodeId))
            {
                if (connections.ContainsKey(connection.NodeId))
                {
                    connections[connection.NodeId].Add(connection.NextNodeId);
                }
            }
        }

        _logger.LogDebug("Built connection graph with {NodeCount} nodes", connections.Count);
        return connections;
    }

    /// <summary>
    /// 计算节点层级（拓扑排序）
    /// </summary>
    private List<List<string>> CalculateNodeLayers(List<AiWorkflowNodeDto> nodes, 
        Dictionary<string, List<string>> connections)
    {
        var layers = new List<List<string>>();
        var remainingNodes = new HashSet<string>(nodes.Select(n => n.NodeId));
        var inDegree = new Dictionary<string, int>();

        // 计算每个节点的入度
        foreach (var nodeId in remainingNodes)
        {
            inDegree[nodeId] = 0;
        }

        foreach (var connection in connections)
        {
            foreach (var targetNodeId in connection.Value)
            {
                if (inDegree.ContainsKey(targetNodeId))
                {
                    inDegree[targetNodeId]++;
                }
            }
        }

        // 分层处理
        while (remainingNodes.Count > 0)
        {
            // 找到当前层的节点（入度为0的节点）
            var currentLayer = remainingNodes.Where(nodeId => inDegree[nodeId] == 0).ToList();
            
            if (currentLayer.Count == 0)
            {
                // 如果没有入度为0的节点，说明有循环依赖，将剩余节点放到一层
                _logger.LogWarning("Detected circular dependencies in workflow, placing remaining nodes in single layer");
                currentLayer = remainingNodes.ToList();
            }

            layers.Add(currentLayer);

            // 从剩余节点中移除当前层节点
            foreach (var nodeId in currentLayer)
            {
                remainingNodes.Remove(nodeId);
                
                // 更新连接到的节点的入度
                foreach (var targetNodeId in connections[nodeId])
                {
                    if (inDegree.ContainsKey(targetNodeId))
                    {
                        inDegree[targetNodeId]--;
                    }
                }
            }
        }

        _logger.LogDebug("Calculated {LayerCount} layers: {LayerSizes}", 
            layers.Count, string.Join(", ", layers.Select(l => l.Count)));

        return layers;
    }

    /// <summary>
    /// 应用高精度层次布局
    /// </summary>
    private void ApplyHighPrecisionLayerLayout(List<AiWorkflowNodeDto> nodes, List<List<string>> layers,
        double startX, double startY, double horizontalSpacing, double verticalSpacing, double nodeWidth, double nodeHeight)
    {
        var nodePositions = new Dictionary<string, (double x, double y)>();
        var random = new Random((int)DateTime.UtcNow.Ticks); // 基于时间的随机数种子

        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var layer = layers[layerIndex];
            
            // 计算基础Y坐标，加入高精度变化
            var baseLayerY = startY + layerIndex * verticalSpacing;
            var layerYVariation = random.NextDouble() * 15.0 - 7.5; // ±7.5像素的随机变化
            var layerY = baseLayerY + layerYVariation;

            // 计算这一层的起始X坐标（居中对齐）- 高精度计算
            var layerStartX = startX;
            if (layer.Count > 1)
            {
                var totalLayerWidth = (layer.Count - 1) * horizontalSpacing;
                layerStartX = startX - totalLayerWidth / 2.0;
            }

            for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
            {
                var nodeId = layer[nodeIndex];
                
                // 计算基础X坐标
                var baseNodeX = layerStartX + nodeIndex * horizontalSpacing;
                
                // 添加高精度偏移和微调
                var nodeXVariation = random.NextDouble() * 20.0 - 10.0; // ±10像素的随机变化
                var nodeYVariation = random.NextDouble() * 10.0 - 5.0;  // ±5像素的随机变化
                
                var finalNodeX = baseNodeX + nodeXVariation;
                var finalNodeY = layerY + nodeYVariation;
                
                // 确保节点不会重叠，添加索引相关的微调
                var indexAdjustment = nodeIndex * 0.618033988749; // 使用黄金比例产生更自然的分布
                finalNodeX += indexAdjustment;
                finalNodeY += Math.Sin(nodeIndex * 0.5) * 2.0; // 添加正弦波形式的微调
                
                nodePositions[nodeId] = (finalNodeX, finalNodeY);
                
                _logger.LogDebug("Layer {LayerIndex}, Node {NodeId}: ({X}, {Y})", 
                    layerIndex, nodeId, finalNodeX, finalNodeY);
            }
        }

        // 应用高精度坐标到节点
        foreach (var node in nodes)
        {
            if (nodePositions.ContainsKey(node.NodeId))
            {
                var (x, y) = nodePositions[node.NodeId];
                
                // 转换为高精度字符串格式
                node.ExtendedData.XPosition = x.ToString("F14"); // 14位小数精度
                node.ExtendedData.YPosition = y.ToString("F14"); // 14位小数精度
                
                _logger.LogDebug("Applied high-precision position to node {NodeId} ({Name}): ({X}, {Y})",
                    node.NodeId, node.Name, node.ExtendedData.XPosition, node.ExtendedData.YPosition);
            }
        }
    }

    /// <summary>
    /// 应用高精度网格布局（备用方案）
    /// </summary>
    private void ApplyHighPrecisionGridLayout(List<AiWorkflowNodeDto> nodes)
    {
        _logger.LogInformation("Applying high precision grid layout to {NodeCount} nodes", nodes.Count);

        const double horizontalSpacing = 200.0;
        const double verticalSpacing = 120.0;
        const double startX = 100.0;
        const double startY = 100.0;
        const int nodesPerRow = 3;

        var random = new Random((int)DateTime.UtcNow.Ticks + nodes.Count); // 不同的随机种子

        for (int i = 0; i < nodes.Count; i++)
        {
            var row = i / nodesPerRow;
            var col = i % nodesPerRow;
            
            // 基础坐标计算
            var baseX = startX + col * horizontalSpacing;
            var baseY = startY + row * verticalSpacing;
            
            // 添加高精度随机偏移
            var xVariation = random.NextDouble() * 25.0 - 12.5; // ±12.5像素变化
            var yVariation = random.NextDouble() * 15.0 - 7.5;  // ±7.5像素变化
            
            // 添加基于索引的微调（使用数学常数创建自然分布）
            var goldenRatio = 1.618033988749;
            var indexOffsetX = (i * goldenRatio % 1.0) * 10.0 - 5.0; // 基于黄金比例的偏移
            var indexOffsetY = Math.Sin(i * 0.7) * 3.0; // 正弦波偏移
            
            var finalX = baseX + xVariation + indexOffsetX;
            var finalY = baseY + yVariation + indexOffsetY;
            
            // 转换为高精度字符串
            nodes[i].ExtendedData.XPosition = finalX.ToString("F14");
            nodes[i].ExtendedData.YPosition = finalY.ToString("F14");
            
            _logger.LogDebug("High precision grid position for node {NodeId}: ({X}, {Y})", 
                nodes[i].NodeId, nodes[i].ExtendedData.XPosition, nodes[i].ExtendedData.YPosition);
        }
    }

    #endregion
} 