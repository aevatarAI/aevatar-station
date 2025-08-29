using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.GEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Workflow.Test.TestGAgents;

[Description("An intelligent AIStreaming Agent")]
[GAgent(nameof(AIStreamingGAgent))]
public class AIStreamingGAgent : GAgentBase<AIStreamingState, AIStreamingStateLogEvent>, IAIStreamingGAgent
{
    private readonly ILogger<AIStreamingGAgent> _logger;
    
    public AIStreamingGAgent(ILogger<AIStreamingGAgent> logger)
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "AIStreamingGAgent");
    }

    public async Task<string> GetContent(Guid requestId)
    {
        return State.ChatMessageMap.GetValueOrDefault(requestId);
    }

    [EventHandler]
    public async Task HandleEventAsync(AIStreamingResponseGEvent @event)
    {
        RaiseEvent(new AIStreamingStateLogEvent()
        {
            RequestId = @event.Context.RequestId,
            Content = @event.ResponseContent
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(AIStreamingState state, StateLogEventBase<AIStreamingStateLogEvent> @event)
    {
        switch (@event)
        {
            case AIStreamingStateLogEvent aiStreamingStateLogEvent:
                if (!state.ChatMessageMap.TryGetValue(aiStreamingStateLogEvent.RequestId, out var value))
                {
                    state.ChatMessageMap[aiStreamingStateLogEvent.RequestId] = aiStreamingStateLogEvent.Content;
                }
                else
                {
                    state.ChatMessageMap[aiStreamingStateLogEvent.RequestId] = value + aiStreamingStateLogEvent.Content;
                }
                break;
        }
    }
}

public interface IAIStreamingGAgent : IGAgent
{
    Task<string> GetContent(Guid requestId);
}

[GenerateSerializer]
public class AIStreamingState : StateBase
{
    [Id(0)] public Dictionary<Guid, string> ChatMessageMap { get; set; } = new Dictionary<Guid, string>();
}

public class AIStreamingStateLogEvent : StateLogEventBase<AIStreamingStateLogEvent>
{
    [Id(0)] public Guid RequestId { get; set; }
    [Id(1)] public string Content { get; set; }
}