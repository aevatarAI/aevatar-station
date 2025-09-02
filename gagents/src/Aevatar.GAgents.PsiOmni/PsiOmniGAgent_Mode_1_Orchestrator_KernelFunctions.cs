using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    /// <summary>
    /// Create a new specialized agent with custom prompt and tools
    /// </summary>
    [KernelFunction("call_new_agent")]
    [Description(
        "Creates a new agent with an initial task. The result will be notified to the given parent agent. Don't use call_agent to send the task again.")]
    public async Task<string> CreateAgentAsync(
        [Description("The ID of the parent agent.")]
        string parentAgentId,
        [Description("The call ID of this call.")]
        string callId,
        [Description("The description of the task sent to the new agent. Please provide all necessary information.")]
        string task
    )
    {
        try
        {
            // Create agent configuration with custom prompt
            var agentConfig = State.Configuration;
            if (agentConfig == null)
            {
                return
                    $"❌ Error: AgentConfiguration not set.";
            }

            // Create and initialize the new agent
            var psi = await _gAgentFactory.GetGAgentAsync("omni", "psi", new PsiOmniGAgentConfig()
            {
                Depth = State.Depth + 1
            });
            var agentId = psi.GetGrainId();
            // There's a publisher tied to each parent agent.
            await PublishAsync(psi.GetGrainId(), new AgentConfigEvent
            {
                Configuration = agentConfig,
                ParentAgentId = parentAgentId
            });

            await PublishAsync(psi.GetGrainId(), new UserMessageEvent
            {
                TargetAgentId = agentId.ToString(),
                CallId = callId,
                Content = task,
                ReplyToAgentId = parentAgentId
            });
            var descriptor = new AgentDescriptor
            {
                AgentId = agentId.ToString(),
                Examples = new List<AgentExample>()
                {
                    new AgentExample
                    {
                        Request = task
                    }
                }
            };

            return
                $"Created the following agent and sent the subtask {callId} to it:\n{JsonSerializer.Serialize(descriptor)}";
        }
        catch (Exception ex)
        {
            var errorMessage = $"❌ Error creating agent for parent {parentAgentId}: {ex.Message}";
            Logger.LogError(ex, "❌ Error creating agent for parent {Parent}", parentAgentId);
            return errorMessage;
        }
    }

    [KernelFunction("call_agent")]
    [Description("Calls any ConfigurableAgentGrain by its ID with a natural language query")]
    public async Task<string> CallAgentAsync(
        [Description("The unique ID of the parent agent making the call.")]
        string parentAgentId,
        [Description("The unique ID of the agent to call."), Required]
        string agentId,
        [Description("The call ID of this call.")]
        string callId,
        [Description("Natural language message to be sent to the agent")]
        string message
    )
    {
        Logger.LogInformation("🔗 Generic agent proxy called for {AgentId} with message: {Message}", agentId, message);

        try
        {
            var targetAgent = await _gAgentFactory.GetGAgentAsync(GrainId.Parse(agentId));

            await PublishAsync(targetAgent.GetGrainId(), new UserMessageEvent
            {
                TargetAgentId = agentId,
                CallId = callId,
                Content = message,
                ReplyToAgentId = parentAgentId
            });

            var call = new AgentCall
            {
                AgentId = agentId,
                CallId = callId,
                Message = message
            };

            return $"Agent call sent: {JsonSerializer.Serialize(call)}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error in generic agent proxy for {AgentId}", agentId);
            return $"Error calling agent '{agentId}': {ex.Message}";
        }
    }
}