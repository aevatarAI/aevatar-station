using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Domain.WorkflowOrchestration;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AI.Common;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Orleans;
using System.Text;
using System.Text.Json;

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
    /// 根据用户目标生成完整的工作流JSON（包含agent发现、prompt构建等所有逻辑）
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(string userGoal);
}

/// <summary>
/// 工作流组合器GAgent - 完全自包含的AI工作流生成器
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
        return Task.FromResult("完全自包含的AI工作流生成器，负责agent发现、prompt构建和AI调用");
    }

    /// <summary>
    /// 根据用户目标生成完整的工作流JSON（包含所有AI相关逻辑）
    /// </summary>
    public async Task<string> GenerateWorkflowJsonAsync(string userGoal)
    {
        try
        {
            Logger.LogInformation("Starting comprehensive AI workflow generation for user goal: {UserGoal}", userGoal);

            // 1. 发现可用的agents
            var availableAgents = await DiscoverAvailableAgentsAsync();
            Logger.LogDebug("Discovered {Count} available agents", availableAgents.Count());

            // 2. 构建完整的AI提示词
            var prompt = BuildWorkflowGenerationPrompt(userGoal, availableAgents);
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
    /// 发现可用的agents
    /// </summary>
    private async Task<IEnumerable<AgentIndexInfo>> DiscoverAvailableAgentsAsync()
    {
        try
        {
            // 通过ServiceProvider获取IAgentIndexService
            var agentIndexService = ServiceProvider.GetService(typeof(IAgentIndexService)) as IAgentIndexService;
            if (agentIndexService == null)
            {
                Logger.LogWarning("IAgentIndexService not available from ServiceProvider");
                return new List<AgentIndexInfo>();
            }

            var agents = await agentIndexService.GetAllAgentsAsync();
            Logger.LogDebug("Agent discovery completed, found {Count} agents", agents.Count());
            return agents;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to discover agents, using empty list");
            return new List<AgentIndexInfo>();
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
        prompt.AppendLine("  \"workflowNodeList\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"agentType\": \"Agent type name (e.g., DataProcessorAgent)\",");
        prompt.AppendLine("      \"name\": \"Node display name\",");
        prompt.AppendLine("      \"extendedData\": {");
        prompt.AppendLine("        \"position_x\": \"Node X coordinate (string format, e.g., '100')\",");
        prompt.AppendLine("        \"position_y\": \"Node Y coordinate (string format, e.g., '100')\",");
        prompt.AppendLine("        \"width\": \"Node width (string format, e.g., '200')\",");
        prompt.AppendLine("        \"height\": \"Node height (string format, e.g., '80')\"");
        prompt.AppendLine("      },");
        prompt.AppendLine("      \"properties\": {");
        prompt.AppendLine("        \"inputParam1\": \"Input parameter value\",");
        prompt.AppendLine("        \"inputParam2\": \"Input parameter value\"");
        prompt.AppendLine("      },");
        prompt.AppendLine("      \"nodeId\": \"Unique node ID (UUID format)\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ],");
        prompt.AppendLine("  \"workflowNodeUnitList\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"nodeId\": \"Current node ID\",");
        prompt.AppendLine("      \"nextnodeId\": \"Next node ID\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ],");
        prompt.AppendLine("  \"Name\": \"Workflow name\"");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("## Important Notes");
        prompt.AppendLine("1. agentType must use the TypeName of the Agent, selected from the available Agent list above");
        prompt.AppendLine("2. All values in extendedData must be in string format");
        prompt.AppendLine("3. Please arrange node positions from left to right, top to bottom, with 150-200 pixel spacing between nodes");
        prompt.AppendLine("4. workflowNodeUnitList defines execution order, each entry indicates the next node to execute after the current node completes");
        prompt.AppendLine("5. properties contains input parameter configuration for the Agent node");

        return prompt.ToString();
    }

    /// <summary>
    /// 获取fallback工作流JSON（统一实现）
    /// </summary>
    private string GetFallbackWorkflowJson()
    {
        Logger.LogInformation("Using fallback workflow JSON");
        return @"{
            ""workflowNodeList"": [
                {
                    ""agentType"": ""DataProcessorAgent"",
                    ""name"": ""Data Processing Node"",
                    ""extendedData"": {
                        ""position_x"": ""100"",
                        ""position_y"": ""100"",
                        ""width"": ""200"",
                        ""height"": ""80""
                    },
                    ""properties"": {
                        ""inputData"": ""User input data"",
                        ""processingMode"": ""batch""
                    },
                    ""nodeId"": ""node-1""
                },
                {
                    ""agentType"": ""OutputAgent"",
                    ""name"": ""Output Node"",
                    ""extendedData"": {
                        ""position_x"": ""350"",
                        ""position_y"": ""100"",
                        ""width"": ""200"",
                        ""height"": ""80""
                    },
                    ""properties"": {
                        ""outputFormat"": ""json""
                    },
                    ""nodeId"": ""node-2""
                }
            ],
            ""workflowNodeUnitList"": [
                {
                    ""nodeId"": ""node-1"",
                    ""nextnodeId"": ""node-2""
                }
            ],
            ""Name"": ""AI Generated Workflow""
        }";
    }
} 