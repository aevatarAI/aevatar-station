using Aevatar.Core;
using GroupChat.GAgent.Feature.Blackboard.LogEvent;
using GroupChat.GAgent.Feature.Common;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Coordinator.GEvent;

namespace GroupChat.GAgent.Feature.Blackboard;

public class BlackboardGAgent : GAgentBase<BlackboardState, BlackboardLogEvent>,
    IBlackboardGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "BlackboardGAgent - A shared memory space for workflow execution. " +
            "Stores conversation history, intermediate results, and enables data sharing between non-connected workflow nodes. " +
            "Acts as the central data repository following the Blackboard architectural pattern."
        );
    }

    public async Task<bool> SetTopic(string topic)
    {
        if (State.MessageList.Count > 0)
        {
            return false;
        }

        RaiseEvent(new AddChatHistoryLogEvent
        {
            MessageType = MessageType.BlackboardTopic,
            Content = topic
        });
        await ConfirmEvents();
        return true;
    }

    public Task<List<ChatMessage>> GetContent()
    {
        return Task.FromResult(State.MessageList);
    }

    public Task<List<ChatMessage>> GetLastChatMessageAsync(List<Guid> talkerList)
    {
        return Task.FromResult(State.MessageList.Where(w => talkerList.Contains(w.MemberId)).ToList());
    }

    public async Task SetMessageAsync(CoordinatorConfirmChatResponse confirmChatResponse)
    {
        await HandleEventAsync(confirmChatResponse);
    }

    public async Task ResetAsync()
    {
        RaiseEvent(new CleanChatHistoryLogEvent());
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(CoordinatorConfirmChatResponse @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey())
        {
            return;
        }

        if (@event.ChatResponse.Skip == false)
        {
            RaiseEvent(new AddChatHistoryLogEvent()
            {
                AgentName = @event.MemberName, MemberId = @event.MemberId, MessageType = MessageType.User,
                Content = @event.ChatResponse.Content
            });
            await ConfirmEvents();
        }
    }

    protected override void GAgentTransitionState(BlackboardState state, StateLogEventBase<BlackboardLogEvent> @event)
    {
        switch (@event)
        {
            case AddChatHistoryLogEvent addChatHistoryLogEvent:
                var message = new ChatMessage
                {
                    AgentName = addChatHistoryLogEvent.AgentName, Content = addChatHistoryLogEvent.Content,
                    MemberId = addChatHistoryLogEvent.MemberId,
                    MessageType = addChatHistoryLogEvent.MessageType
                };

                state.MessageList.Add(message);
                break;
            case CleanChatHistoryLogEvent:
                state.MessageList.Clear();
                break;
        }
    }
}

[GenerateSerializer]
public class BlackboardLogEvent : StateLogEventBase<BlackboardLogEvent>;

[GenerateSerializer]
public class AddChatHistoryLogEvent : BlackboardLogEvent
{
    [Id(0)] public MessageType MessageType { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string AgentName { get; set; }
    [Id(3)] public string Content { get; set; }
}

[GenerateSerializer]
public class CleanChatHistoryLogEvent : BlackboardLogEvent;