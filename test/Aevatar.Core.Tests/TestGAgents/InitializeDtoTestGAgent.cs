using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

[GenerateSerializer]
public class InitializeDtoTestGAgentState : StateBase
{
    [Id(0)]  public List<string> Content { get; set; }
}

public class InitializeDtoTestStateLogEvent : StateLogEventBase<InitializeDtoTestStateLogEvent>
{
    [Id(0)] public Guid Id { get; set; }
}

public class Initialize : InitializationEventBase
{
    [Id(0)] public string InitialGreeting { get; set; }
}

[GAgent("initialize")]
public class InitializeDtoTestGAgent : GAgentBase<InitializeDtoTestGAgentState,
    InitializeDtoTestStateLogEvent,EventBase, Initialize>
{
    public InitializeDtoTestGAgent(ILogger<InitializeDtoTestGAgent> logger) : base(logger)
    {
    }

    public override Task InitializeAsync(Initialize initialize)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(initialize.InitialGreeting);
        return Task.CompletedTask;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a test GAgent for initialization testing.");
    }
}