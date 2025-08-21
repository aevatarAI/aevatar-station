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
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Metadata;
using Newtonsoft.Json;
using JsonException = Newtonsoft.Json.JsonException;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using Newtonsoft.Json.Linq;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Options;
using Volo.Abp;

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
    private readonly IOptionsMonitor<AIServicePromptOptions> _promptOptions;
    private readonly IOptionsMonitor<AgentOptions> _agentOptions;
    private readonly GrainTypeResolver _grainTypeResolver;

    public WorkflowOrchestrationService(
        ILogger<WorkflowOrchestrationService> logger,
        IClusterClient clusterClient,
        IUserAppService userAppService,
        IGAgentManager gAgentManager,
        IOptionsMonitor<AIServicePromptOptions> promptOptions,
        IOptionsMonitor<AgentOptions> agentOptions,
        GrainTypeResolver grainTypeResolver)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _userAppService = userAppService;
        _gAgentManager = gAgentManager;
        _promptOptions = promptOptions;
        _agentOptions = agentOptions;
        _grainTypeResolver = grainTypeResolver;
    }

    /// <summary>
    /// 获取过滤后的业务Agent类型（排除系统Agent）
    /// </summary>
    private List<Type> GetBusinessAgentTypes()
    {
        _logger.LogInformation("=== GetBusinessAgentTypes: Starting agent filtering process ===");
        
        var systemAgents = _agentOptions.CurrentValue.SystemAgentList;
        _logger.LogInformation("System agents to exclude: [{SystemAgents}]", string.Join(", ", systemAgents));
        
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        _logger.LogInformation("Total available GAgent types: {Count}", availableGAgents.Count());
        _logger.LogDebug("All available GAgent types: [{AllAgents}]", 
            string.Join(", ", availableGAgents.Select(a => $"{a.Name}({a.Namespace})")));
        
        var validAgents = availableGAgents.Where(a => !a.Namespace?.StartsWith("OrleansCodeGen") == true).ToList();
        _logger.LogInformation("After filtering OrleansCodeGen: {Count} agents", validAgents.Count);
        
        if (validAgents.Count != availableGAgents.Count())
        {
            var excludedCodeGen = availableGAgents.Where(a => a.Namespace?.StartsWith("OrleansCodeGen") == true);
            _logger.LogDebug("Excluded OrleansCodeGen agents: [{ExcludedAgents}]", 
                string.Join(", ", excludedCodeGen.Select(a => a.Name)));
        }
        
        var businessAgentTypes = validAgents.Where(a => !systemAgents.Contains(a.Name)).ToList();
        _logger.LogInformation("Final business agents after excluding system agents: {Count}", businessAgentTypes.Count);
        _logger.LogInformation("Business agent types: [{BusinessAgents}]", 
            string.Join(", ", businessAgentTypes.Select(a => a.Name)));
        
        if (validAgents.Count != businessAgentTypes.Count)
        {
            var excludedSystemAgents = validAgents.Where(a => systemAgents.Contains(a.Name));
            _logger.LogDebug("Excluded system agents: [{ExcludedSystemAgents}]", 
                string.Join(", ", excludedSystemAgents.Select(a => a.Name)));
        }
        
        _logger.LogInformation("=== GetBusinessAgentTypes: Filtering complete ===");
        return businessAgentTypes;
    }

    /// <summary>
    /// 根据用户目标生成工作流视图配置，直接返回前端可渲染的格式
    /// </summary>
    /// <param name="userGoal">用户目标描述</param>
    /// <returns>前端可渲染的工作流视图配置</returns>
    public async Task<AiWorkflowViewConfigDto?> GenerateWorkflowAsync(string userGoal)
    {
        // Check if user goal is empty or too short - require minimum meaningful description length
        if (string.IsNullOrWhiteSpace(userGoal) || userGoal.Trim().Length < 10)
        {
            _logger.LogWarning("User goal empty or too short for workflow generation: {UserGoal}", userGoal ?? "null");
            throw new UserFriendlyException("Your description is too simple, please provide more detailed generation requirements.");
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
            _logger.LogDebug("Getting business agent types (excluding system agents)");
            var validAgentTypes = GetBusinessAgentTypes();
            
            _logger.LogInformation("Found {TypeCount} business agent types to process", validAgentTypes.Count);
            
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

    /// <summary>
    /// 将AI生成的简单类型名称映射为完整的GrainType名称
    /// </summary>
    private string MapSimpleTypeNameToFullTypeName(string simpleTypeName)
    {
        if (string.IsNullOrEmpty(simpleTypeName) || _grainTypeResolver == null)
            return simpleTypeName;

        var matchedType = GetBusinessAgentTypes()
            .FirstOrDefault(t => t.Name == simpleTypeName);

        return matchedType != null 
            ? _grainTypeResolver.GetGrainType(matchedType).ToString()
            : simpleTypeName;
    }

    #endregion

    #region Private Methods - AI Agent Integration

    /// <summary>
    /// 调用WorkflowComposerGAgent生成工作流JSON（重构：分离用户目标和系统指令）
    /// </summary>
    private async Task<string> CallWorkflowComposerGAgentAsync(string userGoal, Guid userId)
    {
        using var scope = _logger.BeginScope("Operation: CallWorkflowComposerGAgent, UserId: {UserId}", userId);

        try
        {
            _logger.LogInformation("Starting WorkflowComposerGAgent call for user goal: {UserGoal}", userGoal);
            
            // 1. 获取可用的Agent信息
            var availableAgents = await GetAgentDescriptionsAsync();
            _logger.LogInformation("Retrieved {AgentCount} available agents for workflow composition", availableAgents.Count);
            
            if (!availableAgents.Any())
            {
                _logger.LogWarning("No available agents found for workflow composition");
                return CreateEmptyWorkflowJson("无可用Agent");
            }

            // 2. 获取WorkflowComposerGAgent实例
            var workflowComposerGAgent = _clusterClient.GetGrain<IWorkflowComposerGAgent>(Guid.NewGuid());
            
            // 3. 重构：只在InitializeAsync中提供基础系统指令和Agent信息，不包含用户目标
            var baseSystemInstructions = BuildBaseSystemInstructions(availableAgents);
            var initializeDto = new InitializeDto()
            {
                Instructions = baseSystemInstructions,
                LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
            };
            
            _logger.LogInformation("Initializing WorkflowComposerGAgent with base instructions. Agent count: {AgentCount}", availableAgents.Count);
            
            await workflowComposerGAgent.InitializeAsync(initializeDto);
            
            // 4. 传递用户目标给GAgent，让其内部处理prompt构造
            _logger.LogInformation("Generating workflow JSON with user goal: {UserGoal}", userGoal);
            var result = await workflowComposerGAgent.GenerateWorkflowJsonAsync(userGoal);
            
            _logger.LogInformation("WorkflowComposerGAgent completed successfully, result length: {ResultLength}", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WorkflowComposerGAgent call");
            throw; // 让上层处理异常
        }
    }

    /// <summary>
    /// 构建基础系统指令（不包含用户目标，只包含agent信息和角色定义）
    /// </summary>
    private string BuildBaseSystemInstructions(List<AiWorkflowAgentInfoDto> availableAgents)
    {
        try
        {
            _logger.LogDebug("Building base system instructions with {AgentCount} agents", availableAgents.Count);

            var promptBuilder = new StringBuilder();

            // 1. 系统角色定义
            promptBuilder.AppendLine(_promptOptions.CurrentValue.SystemRoleTemplate);
            promptBuilder.AppendLine();

            // 2. 构建Agent目录内容（不包含具体用户目标）
            var agentCatalogContent = BuildAgentCatalogContent(availableAgents);
            var agentCatalogSection = _promptOptions.CurrentValue.AgentCatalogSectionTemplate.Replace("{AGENT_CATALOG_CONTENT}", agentCatalogContent);
            promptBuilder.AppendLine(agentCatalogSection);
            promptBuilder.AppendLine();

            // 3. 输出要求
            promptBuilder.AppendLine(_promptOptions.CurrentValue.OutputRequirementsTemplate);
            promptBuilder.AppendLine();

            // 4. 重要约束条件
            promptBuilder.AppendLine(_promptOptions.CurrentValue.CriticalConstraintsTemplate);
            promptBuilder.AppendLine();

            // 5. JSON格式规范
            promptBuilder.AppendLine(_promptOptions.CurrentValue.JsonFormatSpecificationTemplate);

            var baseInstructions = promptBuilder.ToString();
            _logger.LogDebug("Built base system instructions with length: {PromptLength}", baseInstructions.Length);

            return baseInstructions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building base system instructions");
            // 返回基础的提示词作为后备
            return _promptOptions.CurrentValue.SystemRoleTemplate + "\n\n" + _promptOptions.CurrentValue.JsonFormatSpecificationTemplate;
        }
    }

    /// <summary>
    /// 构建Agent目录内容
    /// </summary>
    private string BuildAgentCatalogContent(List<AiWorkflowAgentInfoDto> availableAgents)
    {
        if (!availableAgents.Any())
        {
            return _promptOptions.CurrentValue.NoAgentsAvailableMessage;
        }

        var catalogBuilder = new StringBuilder();

        foreach (var agent in availableAgents)
        {
            var agentSection = _promptOptions.CurrentValue.SingleAgentTemplate
                .Replace("{AGENT_NAME}", agent.Name)
                .Replace("{AGENT_TYPE}", agent.Type)
                .Replace("{AGENT_DESCRIPTION}", agent.Description);

            catalogBuilder.AppendLine(agentSection);
            catalogBuilder.AppendLine();
        }

        return catalogBuilder.ToString().TrimEnd();
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
            _logger.LogInformation("Start parsing workflow JSON. Content length: {ContentLength}", jsonContent?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty JSON content provided for parsing");
                return null;
            }

            var cleanJson = AiAgentHelper.CleanJsonContent(jsonContent);
            _logger.LogDebug("Parsing workflow JSON content: {CleanJson}", cleanJson);

            var jsonObject = JObject.Parse(cleanJson);

            // Simplified: expect the flat schema as primary, fall back to properties if present
            var workflow = new AiWorkflowViewConfigDto
            {
                Name = jsonObject["name"]?.ToString() ?? "Unnamed Workflow",
                Properties = new AiWorkflowPropertiesDto
                {
                    // Prefer nested properties.name when present, fall back to top-level name
                    Name = (jsonObject["properties"] as JObject)?["name"]?.ToString()
                           ?? jsonObject["name"]?.ToString()
                           ?? "Unnamed Workflow",
                    WorkflowNodeList = new List<AiWorkflowNodeDto>(),
                    WorkflowNodeUnitList = new List<AiWorkflowNodeUnitDto>()
                }
            };

            // Pick node/edge arrays (flat first, then properties wrapper)
            var nodesArray = (jsonObject["workflowNodeList"] as JArray)
                             ?? (jsonObject["properties"] as JObject)?["workflowNodeList"] as JArray
                             ?? new JArray();
            var edgesArray = (jsonObject["workflowNodeUnitList"] as JArray)
                             ?? (jsonObject["properties"] as JObject)?["workflowNodeUnitList"] as JArray
                             ?? new JArray();

            // 创建originalId到GUID的映射
            var nodeIdMapping = new Dictionary<string, string>();
            var nodeIndex = 0;

            // 解析节点并建立映射关系
            foreach (var token in nodesArray.OfType<JObject>())
            {
                // 从JSON中获取原始的node ID
                var originalNodeId = token.Value<string>("nodeId")
                                   ?? $"node_{nodeIndex}"; // 为没有ID的节点创建fallback ID
                
                // 生成新的GUID并建立映射
                var newNodeId = Guid.NewGuid().ToString();
                nodeIdMapping[originalNodeId] = newNodeId;
                _logger.LogDebug("Node ID mapping: {OriginalId} -> {NewGuid}", originalNodeId, newNodeId);
                
                // 从JSON中获取简单的agent类型名称
                var simpleAgentType = token.Value<string>("nodeType")
                                    ?? token.Value<string>("agentType")
                                    ?? string.Empty;
                
                // 将简单类型名称映射为完整的GrainType名称
                var fullAgentType = MapSimpleTypeNameToFullTypeName(simpleAgentType);
                
                // 直接使用GUID作为NodeId
                var node = new AiWorkflowNodeDto
                {
                    NodeId = newNodeId, // 直接使用GUID
                    AgentType = fullAgentType,
                    // Support both nodeName and name as schema variants
                    Name = token.Value<string>("nodeName")
                           ?? token.Value<string>("name")
                           ?? string.Empty,
                    Properties = new Dictionary<string, object>(),
                    ExtendedData = new AiWorkflowNodeExtendedDataDto
                    {
                        XPosition = (nodeIndex * 200 + 100).ToString(), // 横向排列，每个节点间隔200px，起始位置100px
                        YPosition = "100" // 固定Y坐标为100px，保持在同一水平线
                    }
                };

                var desc = token["extendedData"]?[(object)"description"]?.ToString();
                if (!string.IsNullOrEmpty(desc))
                {
                    node.Properties["description"] = desc;
                }

                // copy node properties as-is
                var props = token["properties"] as JObject;
                if (props != null)
                {
                    foreach (var p in props)
                    {
                        node.Properties[p.Key] = p.Value?.ToObject<object>() ?? string.Empty;
                    }
                }

                workflow.Properties.WorkflowNodeList.Add(node);
                nodeIndex++;
            }

            // 解析边并使用映射转换ID
            foreach (var token in edgesArray.OfType<JObject>())
            {
                var originalFromNodeId = token.Value<string>("fromNodeId") ?? string.Empty;
                var originalToNodeId = token.Value<string>("toNodeId") ?? string.Empty;

                // 使用映射转换为GUID
                var mappedFromNodeId = nodeIdMapping.ContainsKey(originalFromNodeId) 
                    ? nodeIdMapping[originalFromNodeId] 
                    : string.Empty;
                var mappedToNodeId = nodeIdMapping.ContainsKey(originalToNodeId) 
                    ? nodeIdMapping[originalToNodeId] 
                    : string.Empty;

                if (!string.IsNullOrEmpty(mappedFromNodeId) && !string.IsNullOrEmpty(mappedToNodeId))
                {
                    var unit = new AiWorkflowNodeUnitDto
                    {
                        NodeId = mappedFromNodeId,     // 使用映射后的GUID
                        NextNodeId = mappedToNodeId    // 使用映射后的GUID
                    };
                    workflow.Properties.WorkflowNodeUnitList.Add(unit);
                }
                else
                {
                    _logger.LogWarning("Skipping edge with unmapped node IDs: fromNodeId={FromNodeId}, toNodeId={ToNodeId}", 
                        originalFromNodeId, originalToNodeId);
                }
            }

            _logger.LogInformation("Parsed workflow: nodes={NodeCount}, connections={ConnCount}, nodeIdMappings={MappingCount}",
                workflow.Properties.WorkflowNodeList.Count, workflow.Properties.WorkflowNodeUnitList.Count, nodeIdMapping.Count);

            // Log all agent types generated by AI for debugging
            var generatedAgentTypes = workflow.Properties.WorkflowNodeList
                .Where(n => !string.IsNullOrEmpty(n.AgentType))
                .Select(n => n.AgentType)
                .Distinct()
                .ToList();
            _logger.LogInformation("AI generated agent types: [{GeneratedAgents}]", string.Join(", ", generatedAgentTypes));



            // Layout
            ApplyIntelligentLayout(workflow.Properties);
            return workflow;
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

    /// <summary>
    /// 创建空的工作流JSON（当没有可用Agent或出现错误时的后备方案）
    /// </summary>
    private string CreateEmptyWorkflowJson(string reason)
    {
        try
        {
            _logger.LogInformation("Creating empty workflow JSON due to: {Reason}", reason);
            
            var emptyWorkflow = new
            {
                generationStatus = "failed",
                clarityScore = 0,
                name = "Empty Workflow",
                properties = new
                {
                    name = "Empty Workflow",
                    workflowNodeList = new object[0],
                    workflowNodeUnitList = new object[0]
                },
                errorInfo = new
                {
                    code = "NO_AGENTS_AVAILABLE",
                    message = reason,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                },
                completionPercentage = 0,
                completionGuidance = $"无法生成工作流：{reason}"
            };

            var result = JsonConvert.SerializeObject(emptyWorkflow, Formatting.Indented);
            _logger.LogDebug("Created empty workflow JSON with length: {JsonLength}", result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating empty workflow JSON");
            return "{}";
        }
    }

    #endregion
} 