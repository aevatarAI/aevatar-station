using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
    /// 使用新方案获取所有可用Agent的丰富描述信息（从grain的GetDescriptionAsync获取JSON）
    /// </summary>
    private async Task<List<AgentDescriptionInfo>> GetAgentDescriptionsAsync()
    {
        using var scope = _logger.BeginScope("Operation: GetAgentDescriptions");
        
        try
        {
            _logger.LogDebug("Getting available agent types from GAgentManager");
            var availableTypes = _gAgentManager.GetAvailableGAgentTypes();
            var validAgentTypes = availableTypes.Where(t => !t.Namespace?.StartsWith("OrleansCodeGen") == true).ToList();
            
            _logger.LogInformation("Found {TypeCount} valid agent types to process", validAgentTypes.Count);
            
            // 记录Agent类型的详细信息
            _logger.LogDebug("Available agent types summary: {@AgentTypesSummary}", 
                new { 
                    TotalCount = availableTypes.Count(),
                    ValidCount = validAgentTypes.Count,
                    ValidTypes = validAgentTypes.Select(t => new { t.Name, t.Namespace }).ToList()
                });
            
            var agentDescriptions = new List<AgentDescriptionInfo>();
            
            foreach (var agentType in validAgentTypes)
            {
                try
                {
                    using var agentScope = _logger.BeginScope("AgentType: {AgentType}", agentType.FullName);
                    _logger.LogDebug("Processing agent type");
                    
                    // 为每个类型创建默认grain实例（遵循AgentService的模式）
                    var grainTypeName = agentType.FullName ?? agentType.Name;
                    var grainId = GrainId.Create(grainTypeName, 
                        GuidUtil.GuidToGrainKey(GuidUtil.StringToGuid("AgentDefaultId")));
                    
                    var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                    
                    // 调用新的JSON格式GetDescriptionAsync
                    var jsonDescription = await agent.GetDescriptionAsync();
                    
                    _logger.LogDebug("Agent returned description: {Description}", jsonDescription);
                    
                    // 尝试反序列化为AgentDescriptionInfo
                    AgentDescriptionInfo? agentInfo = null;
                    try
                    {
                        agentInfo = JsonConvert.DeserializeObject<AgentDescriptionInfo>(jsonDescription);
                        _logger.LogDebug("Successfully deserialized agent description: {@AgentInfo}", agentInfo);
                    }
                    catch (JsonException ex)
                    {
                        // 向后兼容：如果JSON反序列化失败，创建基本的AgentDescriptionInfo
                        _logger.LogDebug("Agent returned legacy text description, creating basic AgentDescriptionInfo. JSON error: {Error}", ex.Message);
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
                        _logger.LogDebug("Created legacy compatibility agent info: {@LegacyAgentInfo}", agentInfo);
                    }
                    
                    if (agentInfo != null)
                    {
                        // 确保基本字段有值
                        if (string.IsNullOrEmpty(agentInfo.Name))
                            agentInfo.Name = agentType.Name;
                        if (string.IsNullOrEmpty(agentInfo.Id))
                            agentInfo.Id = agentType.FullName ?? agentType.Name;
                        
                        agentDescriptions.Add(agentInfo);
                        _logger.LogDebug("Successfully processed agent with final info: {@FinalAgentInfo}", 
                            new { agentInfo.Name, agentInfo.Id, agentInfo.Category, agentInfo.L1Description });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get description for agent type");
                }
            }
            
            _logger.LogInformation("Successfully retrieved {AgentCount} agent descriptions using new JSON method", agentDescriptions.Count);
            
            // 记录最终结果的详细信息
            _logger.LogDebug("Final agent collection results: {@AgentCollectionResults}", 
                new { 
                    TotalAgents = agentDescriptions.Count,
                    AgentsByCategory = agentDescriptions.GroupBy(a => a.Category).ToDictionary(g => g.Key, g => g.Count()),
                    AgentNames = agentDescriptions.Select(a => a.Name).ToList()
                });
            
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
                AgentsByCategory = availableAgents.GroupBy(a => a.Category).ToDictionary(g => g.Key, g => g.Count()),
                AgentsWithCapabilities = availableAgents.Where(a => a.Capabilities?.Any() == true).Count()
            };
            _logger.LogDebug("Available agents summary for workflow generation: {@AgentSummary}", agentSummary);
            
            // 记录具体的Agent能力信息
            foreach (var agent in availableAgents.Where(a => a.Capabilities?.Any() == true))
            {
                _logger.LogDebug("Agent capabilities: {@AgentCapabilities}", 
                    new { agent.Name, agent.Category, agent.Capabilities });
            }
            
            // 2. 创建WorkflowComposerGAgent实例并传递丰富的描述信息，使用GUID主键
            var instanceGuid = Guid.NewGuid(); // 使用GUID而不是字符串
            
            using var agentScope = _logger.BeginScope("WorkflowComposerGAgent: {InstanceGuid}", instanceGuid);
            _logger.LogDebug("Creating WorkflowComposerGAgent instance");
            
            var workflowComposerGAgent = _clusterClient.GetGrain<IWorkflowComposerGAgent>(instanceGuid);
            
            // 关键修复：AIGAgent需要先调用InitializeAsync()进行初始化
            var initializeDto = new InitializeDto()
            {
                Instructions = "You are an expert workflow designer that creates sophisticated agent-based workflows. Generate well-structured JSON configurations for complex multi-agent workflows.",
                LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
            };
            _logger.LogDebug("Initializing WorkflowComposerGAgent with config: {@InitializeConfig}", initializeDto);
            
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
        if (properties?.WorkflowNodeList == null || !properties.WorkflowNodeList.Any())
        {
            _logger.LogWarning("No nodes to layout");
            return;
        }

        _logger.LogInformation("开始应用智能布局算法，节点数量: {NodeCount}", properties.WorkflowNodeList.Count);

        try
        {
            // 计算节点层级
            var layers = CalculateNodeLayers(properties.WorkflowNodeList, 
                BuildNodeConnectionGraph(properties));

            if (layers.Any())
            {
                ApplyHighPrecisionLayerLayout(properties.WorkflowNodeList, layers, properties);
            }
            else
            {
                ApplyHighPrecisionGridLayout(properties.WorkflowNodeList);
            }

            _logger.LogInformation("智能布局算法应用完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "智能布局算法应用失败，回退到网格布局");
            ApplyHighPrecisionGridLayout(properties.WorkflowNodeList);
        }
    }

    private Dictionary<string, List<string>> BuildNodeConnectionGraph(AiWorkflowPropertiesDto properties)
    {
        var graph = new Dictionary<string, List<string>>();
        
        // 初始化所有节点
        foreach (var node in properties.WorkflowNodeList)
        {
            if (!graph.ContainsKey(node.NodeId))
            {
                graph[node.NodeId] = new List<string>();
            }
        }

        // 建立连接关系
        foreach (var connection in properties.WorkflowNodeUnitList ?? new List<AiWorkflowNodeUnitDto>())
        {
            if (!string.IsNullOrEmpty(connection.NodeId) && !string.IsNullOrEmpty(connection.NextNodeId))
            {
                if (!graph.ContainsKey(connection.NodeId))
                {
                    graph[connection.NodeId] = new List<string>();
                }
                
                if (!graph[connection.NodeId].Contains(connection.NextNodeId))
                {
                    graph[connection.NodeId].Add(connection.NextNodeId);
                }
            }
        }

        return graph;
    }

    private List<List<string>> CalculateNodeLayers(List<AiWorkflowNodeDto> nodes,
        Dictionary<string, List<string>> graph)
    {
        var layers = new List<List<string>>();
        var processed = new HashSet<string>();
        var inDegree = new Dictionary<string, int>();

        // 计算入度
        foreach (var node in nodes)
        {
            inDegree[node.NodeId] = 0;
        }

        foreach (var (nodeId, connections) in graph)
        {
            foreach (var nextNodeId in connections)
            {
                if (inDegree.ContainsKey(nextNodeId))
                {
                    inDegree[nextNodeId]++;
                }
            }
        }

        // 拓扑排序生成层级
        while (processed.Count < nodes.Count)
        {
            var currentLayer = new List<string>();
            
            foreach (var node in nodes)
            {
                if (!processed.Contains(node.NodeId) && inDegree[node.NodeId] == 0)
                {
                    currentLayer.Add(node.NodeId);
                }
            }

            if (!currentLayer.Any())
            {
                // 处理循环依赖，选择剩余未处理的节点
                var remaining = nodes.Where(n => !processed.Contains(n.NodeId)).ToList();
                if (remaining.Any())
                {
                    currentLayer.Add(remaining.First().NodeId);
                }
            }

            if (currentLayer.Any())
            {
                layers.Add(currentLayer);
                
                foreach (var nodeId in currentLayer)
                {
                    processed.Add(nodeId);
                    
                    // 更新后续节点的入度
                    if (graph.ContainsKey(nodeId))
                    {
                        foreach (var nextNodeId in graph[nodeId])
                        {
                            if (inDegree.ContainsKey(nextNodeId))
                            {
                                inDegree[nextNodeId]--;
                            }
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }

        return layers;
    }

    private void ApplyHighPrecisionLayerLayout(List<AiWorkflowNodeDto> nodes, List<List<string>> layers,
        AiWorkflowPropertiesDto properties)
    {
        const int layerSpacing = 300;
        const int nodeSpacing = 200;
        const int nodeWidth = 180;
        const int nodeHeight = 120;
        
        var nodePositions = new Dictionary<string, (int x, int y)>();

        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var layer = layers[layerIndex];
            var y = layerIndex * layerSpacing + 100;
            
            // 计算层内节点的总宽度
            var totalWidth = (layer.Count - 1) * nodeSpacing + layer.Count * nodeWidth;
            var startX = Math.Max(50, (1200 - totalWidth) / 2);

            for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
            {
                var nodeId = layer[nodeIndex];
                var x = startX + nodeIndex * (nodeWidth + nodeSpacing);
                nodePositions[nodeId] = (x, y);
            }
        }

        // 应用位置到节点
        foreach (var node in nodes)
        {
            if (nodePositions.TryGetValue(node.NodeId, out var position))
            {
                node.ExtendedData.XPosition = position.x.ToString();
                node.ExtendedData.YPosition = position.y.ToString();
            }
        }

        _logger.LogInformation("高精度层级布局完成，处理了 {LayerCount} 层，共 {NodeCount} 个节点", 
            layers.Count, nodes.Count);
    }

    private void ApplyHighPrecisionGridLayout(List<AiWorkflowNodeDto> nodes)
    {
        const int gridCols = 3;
        const int nodeSpacing = 220;
        const int nodeWidth = 180;
        const int nodeHeight = 120;
        const int startX = 100;
        const int startY = 100;

        for (int i = 0; i < nodes.Count; i++)
        {
            var row = i / gridCols;
            var col = i % gridCols;
            
            var x = startX + col * (nodeWidth + nodeSpacing);
            var y = startY + row * (nodeHeight + nodeSpacing);

            nodes[i].ExtendedData.XPosition = x.ToString();
            nodes[i].ExtendedData.YPosition = y.ToString();
        }

        _logger.LogInformation("高精度网格布局完成，处理了 {NodeCount} 个节点", nodes.Count);
    }

    #endregion
} 