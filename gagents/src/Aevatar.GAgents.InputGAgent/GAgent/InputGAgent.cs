// ABOUTME: This file implements the InputGAgent that returns configured input as ChatResponse
// ABOUTME: Extends GroupMemberGAgentBase to integrate with group chat functionality

using Aevatar.Core.Abstractions;
using Aevatar.Core.Interception;
using Aevatar.GAgents.InputGAgent.Dto;
using Aevatar.GAgents.InputGAgent.GAgent.SEvent;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Orleans.Providers;

[module: Interceptor]

namespace Aevatar.GAgents.InputGAgent.GAgent;

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(InputGAgent))]
public class InputGAgent : MemberGAgentBase<InputGAgentState, InputGAgentLogEvent, EventBase, InputConfigDto>, IInputGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Input agent that returns configured input text");
    }

    [Interceptor(IsWorkflowStep = true)]
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // DEBUG: 添加调试信息来验证方法是否被调用和拦截器状态
        Logger.LogInformation("[DEBUG InputGAgent.GetInterestValueAsync] Method called - WorkflowId={WorkflowId}, BlackboardId={BlackboardId}", 
            WorkflowId, blackboardId);
        
        return Task.FromResult(100);
    }

    [Interceptor(IsWorkflowStep = true)]
    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        // DEBUG: 添加调试信息来验证方法是否被调用和拦截器状态
        Logger.LogInformation("[DEBUG InputGAgent.ChatAsync] Method called - WorkflowId={WorkflowId}, BlackboardId={BlackboardId}, MessagesCount={MessagesCount}", 
            WorkflowId, blackboardId, messages?.Count ?? 0);
        
        var response = new ChatResponse
        {
            Content = State.Input,
            Continue = true,
            Skip = false
        };

        return Task.FromResult(response);
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        return Task.CompletedTask;
    }

    [Interceptor(IsWorkflowStep = true)]
    protected override async Task PerformConfigAsync(InputConfigDto configuration)
    {
        // DEBUG: 添加调试信息来验证方法是否被调用和拦截器状态
        Logger.LogInformation("[DEBUG InputGAgent.PerformConfigAsync] Method called - WorkflowId={WorkflowId}, MemberName={MemberName}, Input={Input}", 
            WorkflowId, configuration?.MemberName, configuration?.Input);
        
        await base.PerformConfigAsync(configuration);
        
        RaiseEvent(new SetInputLogEvent { Input = configuration.Input });
        await ConfirmEvents();
    }

    protected override void MemberTransitionState(InputGAgentState state, StateLogEventBase<InputGAgentLogEvent> @event)
    {
        switch (@event)
        {
            case SetInputLogEvent setInputEvent:
                state.Input = setInputEvent.Input;
                break;
        }
    }
}