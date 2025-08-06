using Microsoft.Extensions.Logging;
using Orleans.Providers;

using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Placement;

using Orleans.Streams;
// using Orleans.Runtime.Placement;
using Orleans.Concurrency;
using System.Diagnostics;

namespace E2E.Grains;

[GenerateSerializer]
public class TestDbEvent : EventBase
{
    [Id(0)] public int Number { get; set; } = 0;
}


[GenerateSerializer]
public class TestDbStateLogEvent : StateLogEventBase<TestDbStateLogEvent>
{
    [Id(0)]public int AddMe { get; set; } = 0;
}

[GenerateSerializer]
public class TestDbGState : BroadcastGState
{
    [Id(0)]public int Count { get; set; } = 0;

    // public void Apply(TestDbStateLogEvent @event)
    // {
    //     Count = Count + @event.AddMe;
    // }
}

public interface ITestDbGAgent : IBroadcastGAgent
{
    Task<int> GetCount();

    Task UnSubWithOutHandle<T>() where T : EventBase;

    Task PublishAsync<T>(GrainId grainId,T @event) where T : EventBase;
}

[KeepAlive]
[SiloNamePatternPlacement("User")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestDbGAgent : BroadcastGAgentBase<TestDbGState, TestDbStateLogEvent>, ITestDbGAgent
{
    private static readonly ActivitySource ActivitySource = new("TestDbGAgent", "1.0.0");
    private readonly ILogger<TestDbGAgent> _logger;

    public TestDbGAgent(ILogger<TestDbGAgent> logger)
    {
        _logger = logger;
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("OnGAgentActivate", ActivityKind.Internal);
        if (activity != null)
        {
            activity.SetTag("grain.type", nameof(TestDbGAgent));
            activity.SetTag("grain.id", this.GetGrainId().ToString());
            activity.SetTag("trace.id", activity.TraceId.ToString());
        }

        await base.OnGAgentActivateAsync(cancellationToken);
        await SubscribeBroadcastEventAsync<TestDbEvent>("TestDbScheduleGAgent", OnAddNumberEvent);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("A TestDb counting agent");
    }

    [EventHandler]
    public async Task OnAddNumberEvent(TestDbEvent @event)
    {
        using var activity = ActivitySource.StartActivity("OnAddNumberEvent", ActivityKind.Consumer);
        if (activity != null)
        {
            activity.SetTag("grain.type", nameof(TestDbGAgent));
            activity.SetTag("grain.id", this.GetGrainId().ToString());
            activity.SetTag("event.number", @event.Number);
            activity.SetTag("event.correlation_id", @event.CorrelationId);
            activity.SetTag("event.publisher", @event.PublisherGrainId.ToString());
            activity.SetTag("trace.id", activity.TraceId.ToString());
            
            _logger.LogInformation("Processing event {EventType} with number {Number} from {Publisher}, trace {TraceId}", 
                nameof(TestDbEvent), @event.Number, @event.PublisherGrainId, activity.TraceId);
        }

        RaiseEvent(new TestDbStateLogEvent() { AddMe = @event.Number });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TestDbGState state, StateLogEventBase<TestDbStateLogEvent> @event)
    {
        switch (@event)
        {
            case TestDbStateLogEvent testDbStateLogEvent:
                State.Count += testDbStateLogEvent.AddMe;
                break;
        }

        base.GAgentTransitionState(state, @event);
    }

    [ReadOnly]
    public Task<int> GetCount()
    {
        using var activity = ActivitySource.StartActivity("GetCount", ActivityKind.Server);
        if (activity != null)
        {
            activity.SetTag("grain.type", nameof(TestDbGAgent));
            activity.SetTag("grain.id", this.GetGrainId().ToString());
            activity.SetTag("state.count", State.Count);
            activity.SetTag("trace.id", activity.TraceId.ToString());
        }
        
        return Task.FromResult(State.Count);
    }

    public async Task UnSubWithOutHandle<T>() where T : EventBase
    {
        using var activity = ActivitySource.StartActivity("UnsubscribeEvent", ActivityKind.Internal);
        if (activity != null)
        {
            activity.SetTag("grain.type", nameof(TestDbGAgent));
            activity.SetTag("grain.id", this.GetGrainId().ToString());
            activity.SetTag("event.type", typeof(T).Name);
            activity.SetTag("trace.id", activity.TraceId.ToString());
            
            _logger.LogInformation("Unsubscribing from {EventType}, grain {GrainId}, trace {TraceId}", 
                typeof(T).Name, this.GetGrainId(), activity.TraceId);
        }

        await UnSubscribeBroadcastAsync<T>("TestDbScheduleGAgent");
    }

    public async Task PublishAsync<T>(GrainId grainId, T @event) where T : EventBase
    {
        using var activity = ActivitySource.StartActivity("PublishEvent", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag("grain.type", nameof(TestDbGAgent));
            activity.SetTag("grain.id", this.GetGrainId().ToString());
            activity.SetTag("target.grain_id", grainId.ToString());
            activity.SetTag("event.type", typeof(T).Name);
            activity.SetTag("trace.id", activity.TraceId.ToString());
            
            _logger.LogInformation("Publishing {EventType} to {TargetGrain}, trace {TraceId}", 
                typeof(T).Name, grainId, activity.TraceId);
        }

        var grainIdString = grainId.ToString();
        var streamId = StreamId.Create(AevatarOptions!.StreamNamespace, grainIdString);
        var stream = StreamProvider.GetStream<EventWrapperBase>(streamId);
        var eventWrapper = new EventWrapper<T>(@event, Guid.NewGuid(), this.GetGrainId());
        await stream.OnNextAsync(eventWrapper);
    }
}

public interface ITestDbScheduleGAgent : IBroadcastGAgent 
{

}

[SiloNamePatternPlacement("Scheduler")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestDbScheduleGAgent : BroadcastGAgentBase<BroadcastGState, TestDbStateLogEvent>, ITestDbScheduleGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("A TestDb scbheduling agent");
    }
}