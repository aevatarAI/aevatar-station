using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Newtonsoft.Json;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;
using WorkflowChatMessage = GroupChat.GAgent.Feature.Common.ChatMessage;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[Description("General-purpose conversational AI agent for group chat contexts, handling messages with history, tool-calls, and configurable instructions.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(ChatAIGAgent))]
public class ChatAIGAgent :
    GroupMemberGAgentBase<ChatAIGAgentState, ChatAIGAgentEvent, EventBase, ChatAIGAgentConfigDto>,
    IChatAIGAgent
{
    private readonly ILogger<ChatAIGAgent> _logger;

    public ChatAIGAgent(ILogger<ChatAIGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat AI Agent for group conversations");
    }

    // Implementation of GroupMemberGAgentBase abstract methods
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // AI chat agent always shows high interest in conversations
        return Task.FromResult(80);
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId,
        List<WorkflowChatMessage>? coordinatorMessages)
    {
        var response = new ChatResponse();

        if (coordinatorMessages == null || coordinatorMessages.Count == 0)
        {
            // Let AI generate a default response based on its Instructions
            _logger.LogInformation($"{State.MemberName} generating default AI response based on Instructions");

            // Use Instructions as base context and let AI say something
            var promptWithInstructions =
                $"{State.PromptTemplate ?? ""} Please say something to start the conversation.";
            var chatWithDetails = await ChatWithHistoryAndToolsAsync(promptWithInstructions);
            var defaultResponse = chatWithDetails.Response;

            // Save conversation to state
            RaiseEvent(new ChatResponseEvent()
            {
                Response = defaultResponse,
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            response.Content = defaultResponse;
            return response;
        }

        // Process the workflow messages
        var userMessage = string.Join(" ", coordinatorMessages.Select(m => m.Content));

        _logger.LogInformation($"{State.MemberName} processing workflow message: {userMessage}");

        // Use real AI through ChatWithHistory method
        var aiMessages = await ChatWithHistoryAndToolsAsync(userMessage);
        var aiResponse = !aiMessages.Response.IsNullOrEmpty()
            ? aiMessages.Response
            : $"{State.MemberName}: I'm having trouble processing your request.";

        // Save conversation to state
        RaiseEvent(new ChatResponseEvent()
        {
            Response = aiResponse,
            Timestamp = DateTime.UtcNow
        });
        await ConfirmEvents();

        response.Content = aiResponse;

        return response;
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        _logger.LogInformation($"{State.MemberName} workflow finished for blackboard {blackboardId}");
        return Task.CompletedTask;
    }

    // IChatAIGAgent interface implementation
    public Task<string> GetLastResponseAsync()
    {
        return Task.FromResult(State.LastResponse ?? "No response yet");
    }

    protected override async Task PerformConfigAsync(ChatAIGAgentConfigDto configuration)
    {
        // Call the base implementation to set MemberName
        await base.PerformConfigAsync(configuration);

        // Initialize the AI agent with the provided configuration
        await InitializeAsync(new InitializeDto
        {
            Instructions = configuration.Instructions,
            LLMConfig = new LLMConfigDto { SystemLLM = configuration.SystemLLM },
            MCPServers = configuration.MCPServers,
            ToolGAgentTypes = configuration.ToolGAgentTypes,
            ToolGAgents = configuration.ToolGAgents,
        });

        _logger.LogDebug("PerformConfigAsync ChatAIGAgent configuration and initialization completed");
    }

    protected override void GroupMemberTransitionState(ChatAIGAgentState state,
        StateLogEventBase<ChatAIGAgentEvent> @event)
    {
        _logger.LogDebug("GroupMemberTransitionState: {data}, type:{type}",
            JsonConvert.SerializeObject(@event), @event.GetType().FullName);

        switch (@event)
        {
            case ChatResponseEvent chatResponseEvent:
                state.LastResponse = chatResponseEvent.Response;
                state.LastActivityTime = chatResponseEvent.Timestamp;
                state.TotalInteractions++;
                break;
        }
    }
}