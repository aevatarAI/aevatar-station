using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;

namespace Aevatar.GAgents.SmartHome;

[GenerateSerializer]
public class SmartHomeInputGAgentState : MemberState
{
    [Id(1)] public string Input { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SmartHomeInputGAgentLogEvent : StateLogEventBase<SmartHomeInputGAgentLogEvent>
{
}

[GenerateSerializer]
public class SetSmartHomeInputLogEvent : StateLogEventBase<SmartHomeInputGAgentLogEvent>
{
    [Id(0)] public string Input { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SmartHomeInputConfigDto : MemberConfigDto
{
    [Id(1)] 
    [Required(ErrorMessage = "Input is required")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Input must be between 1 and 5000 characters")]
    [Description("The input text that will be returned as a chat response when the agent is queried")]
    public string Input { get; set; } = string.Empty;
}

public interface ISmartHomeInputGAgent : IStateGAgent<SmartHomeInputGAgentState>;

[GAgent("smart-home-input", "smart-home")]
public class SmartHomeInputGAgent :
    MemberGAgentBase<SmartHomeInputGAgentState, SmartHomeInputGAgentLogEvent, EventBase, SmartHomeInputConfigDto>,
    ISmartHomeInputGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Input agent that returns configured input text");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(100);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
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

    protected override async Task PerformConfigAsync(SmartHomeInputConfigDto configuration)
    {
        await base.PerformConfigAsync(configuration);

        RaiseEvent(new SetSmartHomeInputLogEvent { Input = configuration.Input });
        await ConfirmEvents();
    }

    protected override void MemberTransitionState(SmartHomeInputGAgentState state,
        StateLogEventBase<SmartHomeInputGAgentLogEvent> @event)
    {
        switch (@event)
        {
            case SetSmartHomeInputLogEvent setInputEvent:
                state.Input = setInputEvent.Input;
                break;
        }
    }
}