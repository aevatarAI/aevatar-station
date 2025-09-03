using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Streams;

namespace Aevatar.GAgents.Executor;

[GenerateSerializer]
public class ResultGAgentState : StateBase
{
    [Id(0)] public string? Result { get; set; }
    [Id(1)] public string? ExecutionId { get; set; }
    [Id(2)] public string? StreamProvider { get; set; }
    [Id(3)] public string? StreamNamespace { get; set; }
    [Id(4)] public Type? ExpectedResultType { get; set; }
}

[GenerateSerializer]
public class ResultArrivedStateLogEvent : ResultGAgentStateLogEvent
{
    [Id(0)] public string Result { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SetExecutionContextGAgentStateLogEvent : ResultGAgentStateLogEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
    [Id(1)] public string StreamProvider { get; set; } = string.Empty;
    [Id(2)] public string StreamNamespace { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SetExpectedResultTypeStateLogEvent : ResultGAgentStateLogEvent
{
    [Id(0)] public Type ExpectedResultType { get; set; }
}

[GenerateSerializer]
public class ResultGAgentStateLogEvent : StateLogEventBase<ResultGAgentStateLogEvent>;

public interface IResultGAgent : IStateGAgent<ResultGAgentState>;

[GenerateSerializer]
public class ResultGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string ExecutionId { get; set; }
    [Id(1)] public string StreamProvider { get; set; }
    [Id(2)] public string StreamNamespace { get; set; }
    [Id(3)] public Type? ExpectedResultType { get; set; }
}

[GAgent("result", "aevatar")]
public class ResultGAgent :
    GAgentBase<ResultGAgentState, ResultGAgentStateLogEvent, EventBase, ResultGAgentConfiguration>,
    IResultGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This is a GAgent for collecting GAgent's event handler execution results with Streams support.");
    }

    protected override async Task PerformConfigAsync(ResultGAgentConfiguration configuration)
    {
        RaiseEvent(new SetExecutionContextGAgentStateLogEvent
        {
            ExecutionId = configuration.ExecutionId,
            StreamProvider = configuration.StreamProvider,
            StreamNamespace = configuration.StreamNamespace
        });
        if (configuration.ExpectedResultType != null)
        {
            RaiseEvent(new SetExpectedResultTypeStateLogEvent
            {
                ExpectedResultType = configuration.ExpectedResultType
            });
        }

        await ConfirmEvents();
    }

    [AllEventHandler]
    public async Task OnResultEvent(EventWrapperBase eventWrapper)
    {
        if (eventWrapper is not EventWrapper<EventBase> typedWrapper)
        {
            return;
        }

        if (typedWrapper.PublisherGrainId.Type == GrainType.Create("Aevatar.Core.PublishingGAgent"))
        {
            return;
        }

        if (State.ExpectedResultType is not null && typedWrapper.Event.GetType() != State.ExpectedResultType)
        {
            return;
        }

        Logger.LogInformation("ResultGAgent received event: {EventType} from {Publisher}",
            typedWrapper.Event.GetType().Name, typedWrapper.PublisherGrainId);

        var result = JsonConvert.SerializeObject(typedWrapper.Event);
        RaiseEvent(new ResultArrivedStateLogEvent
        {
            Result = result
        });
        await ConfirmEvents();

        // Notify result to Orleans Streams
        if (!string.IsNullOrEmpty(State.ExecutionId) &&
            !string.IsNullOrEmpty(State.StreamProvider) &&
            !string.IsNullOrEmpty(State.StreamNamespace))
        {
            try
            {
                var streamProvider = this.GetStreamProvider(State.StreamProvider);
                var resultStream =
                    streamProvider.GetStream<ExecutionCompletedEvent>(State.StreamNamespace, State.ExecutionId);

                await resultStream.OnNextAsync(new ExecutionCompletedEvent
                {
                    ExecutionId = State.ExecutionId,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send result through stream for execution {ExecutionId}",
                    State.ExecutionId);
            }
        }
    }

    protected override void GAgentTransitionState(ResultGAgentState state,
        StateLogEventBase<ResultGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case ResultArrivedStateLogEvent resultEvent:
                state.Result = resultEvent.Result;
                break;
            case SetExecutionContextGAgentStateLogEvent contextEvent:
                state.ExecutionId = contextEvent.ExecutionId;
                state.StreamProvider = contextEvent.StreamProvider;
                state.StreamNamespace = contextEvent.StreamNamespace;
                break;
            case SetExpectedResultTypeStateLogEvent typeEvent:
                state.ExpectedResultType = typeEvent.ExpectedResultType;
                break;
        }
    }
}