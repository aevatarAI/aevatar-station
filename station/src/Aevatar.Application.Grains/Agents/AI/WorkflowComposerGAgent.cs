using Aevatar.Core.Abstractions;
using Aevatar.Domain.WorkflowOrchestration;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AI.Common;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Reflection;
using Aevatar.Options;
using Microsoft.Extensions.Options;
using Aevatar.Schema;
using Orleans.Runtime;
using Orleans.Metadata;
using Newtonsoft.Json;
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
    /// 根据用户目标生成完整的工作流JSON（接受新的AgentDescriptionInfo信息）
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AgentDescriptionInfo> availableAgents);
}

/// <summary>
/// 工作流组合器GAgent - 精简的AI工作流生成器（接受AgentDescriptionInfo信息）
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
        var descriptionInfo = new AgentDescriptionInfo
        {
            Id = "Aevatar.Application.Grains.Agents.AI.WorkflowComposerGAgent",
            Name = "WorkflowComposer",
            Category = "AI",
            L1Description = "AI workflow generation agent that creates complete workflow JSON from user goals and available agent descriptions.",
            L2Description = "Advanced AI agent specialized in workflow orchestration. Analyzes user goals and available agent capabilities to generate optimized workflow configurations with proper node connections and data flow management.",
            Capabilities = new List<string> { "workflow-generation", "agent-orchestration", "json-creation", "ai-analysis" },
            Tags = new List<string> { "workflow", "orchestration", "ai-generation", "json" }
        };
        
        return Task.FromResult(JsonConvert.SerializeObject(descriptionInfo));
    }

    /// <summary>
    /// 根据用户目标和Agent描述信息生成工作流JSON
    /// </summary>
    public async Task<string> GenerateWorkflowJsonAsync(string userGoal, List<AgentDescriptionInfo> availableAgents)
    {
        Logger.LogInformation("Starting workflow generation for goal: {UserGoal} with {AgentCount} available agents", 
            userGoal, availableAgents.Count);

        try
        {
            // 直接使用AgentDescriptionInfo生成Prompt
            var prompt = BuildPromptFromAgentDescriptionInfo(userGoal, availableAgents);
            Logger.LogDebug("Generated prompt with length: {PromptLength}", prompt.Length);

            // 调用AI生成工作流
            var aiResult = await CallAIForWorkflowGenerationAsync(prompt);
            Logger.LogInformation("Successfully generated workflow JSON with length: {ResultLength}", aiResult.Length);

            return aiResult;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating workflow JSON for goal: {UserGoal}", userGoal);
            throw;
        }
    }
    
    /// <summary>
    /// 基于AgentDescriptionInfo构建AI提示词
    /// </summary>
    private string BuildPromptFromAgentDescriptionInfo(string userGoal, List<AgentDescriptionInfo> agentList)
    {
        var prompt = new StringBuilder();

        // System role definition
        prompt.AppendLine("# Advanced Workflow Orchestration Expert");
        prompt.AppendLine("You are an advanced AI workflow orchestration expert. Based on user goals, analyze available Agent capabilities and design a complete workflow execution plan with proper node connections and data flow.");
        prompt.AppendLine();

        // User goal
        prompt.AppendLine("## User Goal");
        prompt.AppendLine($"{userGoal}");
        prompt.AppendLine();

        // Available Agent list with rich information
        prompt.AppendLine("## Available Agent Catalog");
        if (agentList.Any())
        {
            foreach (var agent in agentList)
            {
                prompt.AppendLine($"### {agent.Name}");
                prompt.AppendLine($"**Type**: {agent.Id}");
                prompt.AppendLine($"**Quick Description**: {agent.L1Description}");
                prompt.AppendLine($"**Detailed Capabilities**: {agent.L2Description}");
                prompt.AppendLine($"**Category**: {agent.Category}");
                
                if (agent.Tags?.Any() == true)
                    prompt.AppendLine($"**Tags**: {string.Join(", ", agent.Tags)}");
                
                if (agent.Capabilities?.Any() == true)
                {
                    prompt.AppendLine($"**Capabilities**: {string.Join(", ", agent.Capabilities)}");
                }
                
                prompt.AppendLine();
            }
        }
        else
        {
            prompt.AppendLine("No available Agents found");
            prompt.AppendLine();
        }

        // Enhanced output requirements
        prompt.AppendLine("## Advanced Output Requirements");
        prompt.AppendLine("Please analyze the user goal and available agents to create an optimized workflow:");
        prompt.AppendLine("1) **Agent Selection**: Choose the most suitable agents based on capabilities and categories");
        prompt.AppendLine("2) **Node Design**: Create workflow nodes with proper configuration");
        prompt.AppendLine("3) **Connection Logic**: Define execution order and data flow between nodes");
        prompt.AppendLine("4) **Error Handling**: Consider failure scenarios and alternative paths");
        prompt.AppendLine("5) **Performance**: Optimize for execution time and resource usage");
        prompt.AppendLine();

        // JSON format specification
        prompt.AppendLine("## JSON Format Specification");
        prompt.AppendLine("Please strictly follow the following JSON format output:");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"name\": \"User Goal Workflow\",");
        prompt.AppendLine("  \"properties\": {");
        prompt.AppendLine("    \"name\": \"User Goal Workflow\",");
        prompt.AppendLine("    \"workflowNodeList\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"nodeId\": \"unique_node_id\",");
        prompt.AppendLine("        \"nodeName\": \"descriptive_node_name\",");
        prompt.AppendLine("        \"nodeType\": \"agent_type_name\",");
        prompt.AppendLine("        \"extendedData\": {");
        prompt.AppendLine("          \"description\": \"node_purpose_description\"");
        prompt.AppendLine("        },");
        prompt.AppendLine("        \"properties\": {");
        prompt.AppendLine("          \"config_key\": \"config_value\"");
        prompt.AppendLine("        }");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ],");
        prompt.AppendLine("    \"workflowNodeUnitList\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"fromNodeId\": \"source_node_id\",");
        prompt.AppendLine("        \"toNodeId\": \"target_node_id\",");
        prompt.AppendLine("        \"connectionType\": \"data_flow_or_sequence\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("**IMPORTANT**: Only output the JSON, no additional text or explanations.");

        return prompt.ToString();
    }

    /// <summary>
    /// 调用AI服务生成工作流
    /// </summary>
    private async Task<string> CallAIForWorkflowGenerationAsync(string prompt)
    {
        try
        {
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

            Logger.LogDebug("AI successfully generated workflow with response length: {Length}", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling AI for workflow generation");
            return GetFallbackWorkflowJson();
        }
    }

    /// <summary>
    /// 获取回退工作流JSON（当AI生成失败时使用）
    /// </summary>
    private string GetFallbackWorkflowJson()
    {
        return @"{
  ""name"": ""Fallback Workflow"",
  ""properties"": {
    ""name"": ""Fallback Workflow"",
    ""workflowNodeList"": [
      {
        ""nodeId"": ""fallback-node-1"",
        ""nodeName"": ""Default Processing Node"",
        ""nodeType"": ""DefaultAgent"",
        ""extendedData"": {
          ""description"": ""Default processing node when workflow generation fails""
        },
        ""properties"": {
          ""message"": ""This is a fallback workflow generated when AI service is unavailable""
        }
      }
    ],
    ""workflowNodeUnitList"": []
  }
}";
    }
} 