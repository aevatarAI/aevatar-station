using Aevatar.Core.Abstractions;
using Aevatar.Domain.WorkflowOrchestration;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Reflection;
using Aevatar.Options;
using Microsoft.Extensions.Options;
using Aevatar.Schema;
using Orleans.Runtime;
using Orleans.Metadata;
using Newtonsoft.Json;
using Aevatar.Agent;
using System.Security.Cryptography;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 工作流组合器State - 继承AI状态基类
/// </summary>
[GenerateSerializer]
public class WorkflowComposerState : AIGAgentStateBase
{
    // AI状态基类已包含必要的AI功能状态
}

/// <summary>
/// 工作流组合器事件
/// </summary>
[GenerateSerializer]
public class WorkflowComposerEvent : StateLogEventBase<WorkflowComposerEvent>
{
    // 简单事件实现
}

/// <summary>
/// 工作流组合器GAgent接口
/// </summary>
public interface IWorkflowComposerGAgent : IAIGAgent, IGAgent, IGrainWithStringKey
{
    /// <summary>
    /// 根据用户目标生成完整的工作流JSON（接受外部传入的agent信息）
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AgentTypeDto> availableAgents);
}

/// <summary>
/// 工作流组合器GAgent - 精简的AI工作流生成器（agent信息由外部传入）
/// </summary>
[GAgent("WorkflowComposer")]
public class WorkflowComposerGAgent : AIGAgentBase<WorkflowComposerState, WorkflowComposerEvent>, 
    IWorkflowComposerGAgent, IGrainWithStringKey
{
    public WorkflowComposerGAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("精简的AI工作流生成器，接受外部agent信息并负责prompt构建和AI调用");
    }





    /// <summary>
    /// 将AgentTypeDto列表转换为AgentIndexInfo列表（用于内部处理）
    /// </summary>
    private IEnumerable<AgentIndexInfo> ConvertAgentTypeDtoToAgentIndexInfo(List<AgentTypeDto> agentTypeDtos)
    {
        var agentIndexInfos = new List<AgentIndexInfo>();
        
        foreach (var agentDto in agentTypeDtos)
        {
            try
            {
                var agentInfo = new AgentIndexInfo
                {
                    Name = agentDto.AgentType,
                    TypeName = agentDto.AgentType,
                    L1Description = $"Agent: {agentDto.AgentType}",
                    L2Description = !string.IsNullOrWhiteSpace(agentDto.PropertyJsonSchema) 
                        ? $"Agent {agentDto.AgentType} with configuration schema. Parameters: {string.Join(", ", agentDto.AgentParams?.Select(p => $"{p.Name}({p.Type})") ?? new string[0])}"
                        : $"Agent {agentDto.AgentType} without configuration schema.",
                    Categories = InferAgentCategories(agentDto.AgentType, agentDto.FullName),
                    EstimatedExecutionTime = 1000, // 默认估计时间
                    InputParameters = ConvertParamDtosToParameterInfo(agentDto.AgentParams),
                    OutputParameters = new Dictionary<string, AgentParameterInfo>(),
                    CreatedAt = DateTime.UtcNow,
                    LastScannedAt = DateTime.UtcNow
                };
                
                agentIndexInfos.Add(agentInfo);
                Logger.LogDebug("Converted AgentTypeDto {AgentType} to AgentIndexInfo", agentDto.AgentType);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to convert AgentTypeDto {AgentType} to AgentIndexInfo", agentDto.AgentType);
            }
        }
        
        return agentIndexInfos;
    }

    /// <summary>
    /// 将ParamDto列表转换为AgentParameterInfo字典
    /// </summary>
    private Dictionary<string, AgentParameterInfo> ConvertParamDtosToParameterInfo(List<ParamDto>? paramDtos)
    {
        var parameterInfos = new Dictionary<string, AgentParameterInfo>();
        
        if (paramDtos != null)
        {
            foreach (var param in paramDtos)
            {
                parameterInfos[param.Name] = new AgentParameterInfo
                {
                    Type = param.Type,
                    Description = $"Parameter {param.Name} of type {param.Type}",
                    DefaultValue = null
                };
            }
        }
        
        return parameterInfos;
    }

    /// <summary>
    /// 推断Agent类别（基于名称和类型）
    /// </summary>
    private List<string> InferAgentCategories(string agentType, string? fullName)
    {
        var categories = new List<string>();
        var lowerAgentType = agentType.ToLower();
        var lowerFullName = fullName?.ToLower() ?? "";
        
        if (lowerAgentType.Contains("ai") || lowerAgentType.Contains("chat") || lowerAgentType.Contains("llm"))
            categories.Add("AI");
        if (lowerAgentType.Contains("data") || lowerAgentType.Contains("db") || lowerAgentType.Contains("storage"))
            categories.Add("Data");
        if (lowerAgentType.Contains("workflow") || lowerAgentType.Contains("orchestration"))
            categories.Add("Workflow");
        if (lowerAgentType.Contains("messaging") || lowerAgentType.Contains("notification"))
            categories.Add("Communication");
        if (lowerAgentType.Contains("plugin") || lowerAgentType.Contains("extension"))
            categories.Add("Plugin");
        if (lowerAgentType.Contains("test") || lowerAgentType.Contains("demo"))
            categories.Add("Testing");
            
        if (categories.Count == 0)
            categories.Add("General");
            
        return categories;
    }

    /// <summary>
    /// 根据用户目标生成完整的工作流JSON（包含所有AI相关逻辑）
    /// </summary>
    public async Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AgentTypeDto> availableAgents)
    {
        try
        {
            Logger.LogInformation("Starting comprehensive AI workflow generation for user goal: {UserGoal} with {AgentCount} available agents", userGoal, availableAgents.Count);

            // 1. 将AgentTypeDto转换为内部使用的格式
            var agentIndexInfos = ConvertAgentTypeDtoToAgentIndexInfo(availableAgents);
            Logger.LogDebug("Converted {Count} agents to internal format", agentIndexInfos.Count());

            // 2. 构建完整的AI提示词
            var prompt = BuildWorkflowGenerationPrompt(userGoal, agentIndexInfos);
            Logger.LogDebug("Built prompt with length: {Length}", prompt.Length);

            // 3. 调用AI生成工作流
            var chatResult = await ChatWithHistory(prompt);

            if (chatResult == null || chatResult.Count == 0)
            {
                Logger.LogWarning("AI returned empty response for workflow generation");
                return GetFallbackWorkflowJson();
            }

            var response = chatResult[0].Content;
            if (string.IsNullOrWhiteSpace(response))
            {
                Logger.LogWarning("AI returned empty content for workflow generation");
                return GetFallbackWorkflowJson();
            }

            Logger.LogInformation("AI workflow generation completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during comprehensive AI workflow generation");
            return GetFallbackWorkflowJson();
        }
    }



    /// <summary>
    /// 构建工作流生成提示词（完整版本）
    /// </summary>
    private string BuildWorkflowGenerationPrompt(string userGoal, IEnumerable<AgentIndexInfo> availableAgents)
    {
        var agentList = availableAgents?.ToList() ?? new List<AgentIndexInfo>();
        var prompt = new StringBuilder();

        // System role definition
        prompt.AppendLine("# Workflow Orchestration Expert");
        prompt.AppendLine("You are a professional AI workflow orchestration expert. Based on user goals, select appropriate Agents from the provided list and design a complete workflow execution plan.");
        prompt.AppendLine();

        // User goal
        prompt.AppendLine("## User Goal");
        prompt.AppendLine($"{userGoal}");
        prompt.AppendLine();

        // Available Agent list
        prompt.AppendLine("## Available Agent List");
        if (agentList.Any())
        {
            foreach (var agent in agentList)
            {
                prompt.AppendLine($"### {agent.Name} (TypeName: {agent.TypeName})");
                prompt.AppendLine($"**Brief**: {agent.L1Description}");
                prompt.AppendLine($"**Detailed**: {agent.L2Description}");
                prompt.AppendLine($"**Categories**: {string.Join(", ", agent.Categories)}");
                prompt.AppendLine($"**Execution Time**: {agent.EstimatedExecutionTime}ms");
                prompt.AppendLine();
            }
        }
        else
        {
            prompt.AppendLine("No available Agents");
            prompt.AppendLine();
        }

        // Output requirements and JSON format
        prompt.AppendLine("## Output Requirements");
        prompt.AppendLine("Please output complete workflow JSON including: 1) Select appropriate Agents from the above list, 2) Design nodes, 3) Define connection relationships and execution order, 4) Configure data flow between nodes.");
        prompt.AppendLine();

        // JSON format specification
        prompt.AppendLine("## JSON Format Specification");
        prompt.AppendLine("Please strictly follow the following JSON format output:");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"name\": \"Workflow name\",");
        prompt.AppendLine("  \"properties\": {");
        prompt.AppendLine("    \"workflowNodeList\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"nodeId\": \"Unique node ID (UUID format)\",");
        prompt.AppendLine("        \"agentType\": \"Agent type name (e.g., DataProcessorAgent)\",");
        prompt.AppendLine("        \"name\": \"Node display name\",");
        prompt.AppendLine("        \"extendedData\": {");
        prompt.AppendLine("          \"xPosition\": \"Node X coordinate with high precision (e.g., '-120.45763892134567' or '235.78901234567890')\",");
        prompt.AppendLine("          \"yPosition\": \"Node Y coordinate with high precision (e.g., '349.26758722574635' or '-156.89012345678901')\"");
        prompt.AppendLine("        },");
        prompt.AppendLine("        \"properties\": {");
        prompt.AppendLine("          \"inputParam1\": \"Input parameter value\",");
        prompt.AppendLine("          \"inputParam2\": \"Input parameter value\"");
        prompt.AppendLine("        }");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ],");
        prompt.AppendLine("    \"workflowNodeUnitList\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"nodeId\": \"Current node ID\",");
        prompt.AppendLine("        \"nextNodeId\": \"Next node ID\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ],");
        prompt.AppendLine("    \"name\": \"Workflow name\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // Coordinate Generation Rules
        prompt.AppendLine("## Coordinate Generation Rules");
        prompt.AppendLine("When generating node coordinates (xPosition and yPosition), please follow these guidelines for proper four-quadrant distribution:");
        prompt.AppendLine();
        prompt.AppendLine("### Quadrant Distribution:");
        prompt.AppendLine("- **First Quadrant**: x > 0, y > 0 (positive x, positive y)");
        prompt.AppendLine("- **Second Quadrant**: x < 0, y > 0 (negative x, positive y)");
        prompt.AppendLine("- **Third Quadrant**: x < 0, y < 0 (negative x, negative y)");
        prompt.AppendLine("- **Fourth Quadrant**: x > 0, y < 0 (positive x, negative y)");
        prompt.AppendLine();
        prompt.AppendLine("### Coordinate Format Requirements:");
        prompt.AppendLine("1. **High Precision**: Use decimal coordinates with high precision (e.g., \"-35.03630493437581\", \"349.26758722574635\")");
        prompt.AppendLine("2. **String Format**: All coordinate values must be strings, not numbers");
        prompt.AppendLine("3. **Range**: Suggested coordinate range is -500 to +500 for both x and y axes");
        prompt.AppendLine("4. **Distribution**: Try to distribute nodes evenly across all four quadrants when possible");
        prompt.AppendLine("5. **Spacing**: Maintain adequate spacing between nodes (minimum 150-200 units apart)");
        prompt.AppendLine();
        prompt.AppendLine("### Example Coordinate Values:");
        prompt.AppendLine("- First Quadrant: xPosition: \"235.78901234567890\", yPosition: \"289.12345678901234\"");
        prompt.AppendLine("- Second Quadrant: xPosition: \"-156.89012345678901\", yPosition: \"367.45678901234567\"");
        prompt.AppendLine("- Third Quadrant: xPosition: \"-278.34567890123456\", yPosition: \"-189.67890123456789\"");
        prompt.AppendLine("- Fourth Quadrant: xPosition: \"312.56789012345678\", yPosition: \"-245.90123456789012\"");
        prompt.AppendLine();

        prompt.AppendLine("## Important Notes");
        prompt.AppendLine("1. agentType must use the TypeName of the Agent, selected from the available Agent list above");
        prompt.AppendLine("2. All values in extendedData must be in string format");
        prompt.AppendLine("3. Coordinates should be distributed across four quadrants with high-precision decimal values");
        prompt.AppendLine("4. workflowNodeUnitList defines execution order, each entry indicates the next node to execute after the current node completes");
        prompt.AppendLine("5. properties contains input parameter configuration for the Agent node");
        prompt.AppendLine("6. Use xPosition and yPosition for coordinates (not position_x/position_y)");
        prompt.AppendLine("7. Use nextNodeId (not nextnodeId) for node connections");
        prompt.AppendLine("8. Ensure coordinate values are realistic and maintain proper spacing between nodes");

        return prompt.ToString();
    }

    /// <summary>
    /// 获取fallback工作流JSON（统一实现）
    /// </summary>
    private string GetFallbackWorkflowJson()
    {
        Logger.LogInformation("Using fallback workflow JSON");
        return @"{
            ""name"": ""AI Generated Workflow"",
            ""properties"": {
                ""workflowNodeList"": [
                    {
                        ""nodeId"": ""node-1"",
                        ""agentType"": ""DataProcessorAgent"",
                        ""name"": ""Data Processing Node"",
                        ""extendedData"": {
                            ""xPosition"": ""156.78901234567890"",
                            ""yPosition"": ""234.56789012345678""
                        },
                        ""properties"": {
                            ""inputData"": ""User input data"",
                            ""processingMode"": ""batch""
                        }
                    },
                    {
                        ""nodeId"": ""node-2"",
                        ""agentType"": ""OutputAgent"",
                        ""name"": ""Output Node"",
                        ""extendedData"": {
                            ""xPosition"": ""-203.45678901234567"",
                            ""yPosition"": ""-178.90123456789012""
                        },
                        ""properties"": {
                            ""outputFormat"": ""json""
                        }
                    }
                ],
                ""workflowNodeUnitList"": [
                    {
                        ""nodeId"": ""node-1"",
                        ""nextNodeId"": ""node-2""
                    }
                ],
                ""name"": ""AI Generated Workflow""
            }
        }";
    }
} 