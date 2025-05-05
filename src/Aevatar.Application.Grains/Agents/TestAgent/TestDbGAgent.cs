
using Microsoft.Extensions.Logging;
using Orleans.Providers;

using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Placement;

using Orleans.Streams;
using Orleans.Concurrency;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[GenerateSerializer]
public class TestDbEvent : EventBase
{
    [Id(0)] public int Number { get; set; } = 0;
}


[GenerateSerializer]
public class TestDbStateLogEvent : StateLogEventBase<TestDbStateLogEvent>
{
    public int AddMe { get; set; } = 0;
}

[GenerateSerializer]
public class TestDbGState : BroadCastGState
{
    public int Count { get; set; } = 0;

    public void Apply(TestDbStateLogEvent @event)
    {
        Count = Count + @event.AddMe;
    }
}
public interface ITestDbGAgent : IBroadCastGAgent
{
    Task<int> GetCount();

    Task UnSubWithOutHandle<T>() where T : EventBase;

    Task PublishAsync<T>(GrainId grainId,T @event) where T : EventBase;
}

[SiloNamePatternPlacement("User")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestDbGAgent : BroadCastGAgentBase<TestDbGState, TestDbStateLogEvent>, ITestDbGAgent
{
    public async Task PublishAsync<T>(GrainId grainId,T @event) where T : EventBase
    {
        var grainIdString = grainId.ToString();
        var streamId = StreamId.Create(AevatarOptions!.StreamNamespace, grainIdString);
        var stream = StreamProvider.GetStream<EventWrapperBase>(streamId);
        var eventWrapper = new EventWrapper<T>(@event, Guid.NewGuid(), this.GetGrainId());
        await stream.OnNextAsync(eventWrapper);
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);

        await SubscribeBroadCastEventAsync<TestDbEvent>("TestDbScheduleGAgent", OnAddNumberEvent);
    }
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("A TestDb counting agent");
    }

    [EventHandler]
    public async Task OnAddNumberEvent(TestDbEvent @event)
    {
        RaiseEvent(new TestDbStateLogEvent() { AddMe = @event.Number });
        // Logger.LogInformation($"TestDbGAgent received event {@event}");
        await ConfirmEvents();
    }

    [ReadOnly]
    public Task<int> GetCount()
    {
        // Logger.LogInformation($"GetCount called, current count is {State.Count}");
        return Task.FromResult(State.Count);
    }

    public async Task UnSubWithOutHandle<T>() where T : EventBase
    {
        // Logger.LogInformation($"UnSub called");
        await UnSubscribeBroadCastAsync<T>("TestDbScheduleGAgent");
    }

}

public interface ITestDbScheduleGAgent : IBroadCastGAgent 
{

}

[SiloNamePatternPlacement("Scheduler")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestDbScheduleGAgent : BroadCastGAgentBase<BroadCastGState, TestDbStateLogEvent>, ITestDbScheduleGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("A TestDb scbheduling agent");
    }
}