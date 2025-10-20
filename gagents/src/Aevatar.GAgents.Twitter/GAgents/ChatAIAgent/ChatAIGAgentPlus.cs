using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Newtonsoft.Json;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;
using WorkflowChatMessage = GroupChat.GAgent.Feature.Common.ChatMessage;
using Aevatar.Core.Placement;
using GroupChat.GAgent.Feature.Blackboard;
using Aevatar.GAgents.AIGAgent.Agent;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[SiloNamePatternPlacement("Projector")]
[Description("General-purpose conversational AI agent for group chat contexts, handling messages with history, tool-calls, and configurable instructions.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(ChatAIGAgentPlus))]
public class ChatAIGAgentPlus :
    AIGAgentBasePlus<ChatAIGAgentStatePlus, ChatAIGAgentEvent, ChatAIGAgentConfigDtoPlus>,
    IChatAIGAgentPlus
{
    private readonly ILogger<ChatAIGAgentPlus> _logger;

    public ChatAIGAgentPlus(ILogger<ChatAIGAgentPlus> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat AI Agent for group conversations");
    }



    // IChatAIGAgent interface implementation
    public Task<string> GetLastResponseAsync()
    {
        return Task.FromResult(State.LastResponse ?? "No response yet");
    }


    protected override async Task PerformConfigAsync(ChatAIGAgentConfigDtoPlus configuration)
    {
        // Call the base implementation to set MemberName
        await base.PerformConfigAsync(configuration);

        // Initialize the AI agent with the provided configuration
        await InitializeAsync(new InitializeDto
        {
            Instructions = configuration.Instructions,
            LLMConfig = new LLMConfigDto { SystemLLM = configuration.SystemLLM.ToString() },
            MCPServers = configuration.MCPServers,
            ToolGAgentTypes = configuration.ToolGAgentTypes,
            ToolGAgents = configuration.ToolGAgents,
        });

        _logger.LogDebug($"[{this.GetGrainId()}] PerformConfigAsync ChatAIGAgent configuration and initialization completed");
    }

    /// <summary>
    /// ✅ UPDATED: Override OnBusinessAgentEventForwardingEventHandlerAsync instead of OnEventForwardingEventHandlerAsync
    /// This ensures BusinessAgentBase validation runs first
    /// Simplified: just check if there's a message to process with AI
    /// </summary>
    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        _logger.LogInformation("ChatAIGAgent {AgentId} received WorkflowEvent", this.GetPrimaryKey());

        try
        {
            // Assign the WorkUnitAgentId to represent this processing node
            workflowEvent.WorkUnitAgentId = this.GetGrainId().ToString();
            
            // Check if there's a message to process with AI
            if (!string.IsNullOrEmpty(workflowEvent.Message))
            {
                await ProcessChatTaskAsync(workflowEvent);
            }
            else
            {
                _logger.LogDebug("ChatAIGAgent {AgentId} received event with no message to process",
                    this.GetPrimaryKey());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatAIGAgent {AgentId} failed to process WorkflowEvent", this.GetPrimaryKey());

            // Update workflow event with error
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowFailed;
            workflowEvent.ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Process chat task using AI capabilities - converted from ChatAsync logic
    /// </summary>
    private async Task ProcessChatTaskAsync(WorkflowEvent workflowEvent)
    {
        try
        {
            _logger.LogInformation("ChatAIGAgent {AgentId} processing chat task with message: {Message}",
                this.GetPrimaryKey(), workflowEvent.Message);

            // Extract message from WorkflowEvent.Message (passed from input agents)
            var inputMessage = _receivedMessages.Count > 0 
                ? string.Join(" ", _receivedMessages) 
                : "Please provide a response.";
            // var inputMessage = workflowEvent.Message ?? "Please provide a response.";
            // Use PromptTemplate from the base state instead of Instructions
            if (!string.IsNullOrEmpty(State.PromptTemplate))
            {
                inputMessage += " " + State.PromptTemplate;
            }

            // Use AI to generate response
            var aiResponse = await ChatWithHistoryAndToolsAsync(inputMessage);
            var responseContent = !string.IsNullOrEmpty(aiResponse.Response)
                ? aiResponse.Response
                : "I'm having trouble processing your request.";

            // ✅ FIXED: Use event sourcing to update state
            RaiseEvent(new ChatResponseEvent
            {
                Response = responseContent,
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            // Update workflow event with AI response
            workflowEvent.Message = responseContent;
            workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Completed;
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowInProgress;

            _logger.LogInformation("ChatAIGAgent {AgentId} completed chat task with response: {Response}",
                this.GetPrimaryKey(), responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatAIGAgent {AgentId} failed to process chat task", this.GetPrimaryKey());

            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowFailed;
            workflowEvent.ErrorMessage = ex.Message;
            workflowEvent.Message = "I encountered an error processing your request.";
        }
    }

    protected override void AIGAgentTransitionState(ChatAIGAgentStatePlus state, StateLogEventBase<ChatAIGAgentEvent> @event)
    {
        // Handle ChatAI-specific events
        switch (@event)
        {
            case ChatResponseEvent chatResponseEvent:
                state.LastResponse = chatResponseEvent.Response;
                state.LastActivityTime = chatResponseEvent.Timestamp;
                state.TotalInteractions++;
                break;
                
            case SetInitialPromptEvent setPromptEvent:
                // Handle initial prompt setup if needed
                break;
        }
        
        // CRITICAL: Call base implementation to handle AI-specific events
        base.AIGAgentTransitionState(state, @event);
    }

}