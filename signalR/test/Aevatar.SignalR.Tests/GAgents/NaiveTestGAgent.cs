using Aevatar.Core;
using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.Tests.GAgents;

[GenerateSerializer]
public class NaiveTestGAgentState : StateBase
{
    [Id(0)]  public List<string> Content { get; set; }
}

public class NaiveTestStateLogEvent : StateLogEventBase<NaiveTestStateLogEvent>
{
    [Id(0)] public Guid Id { get; set; }
}

[GAgent("naiveTest")]
public class
    NaiveTestGAgent : GAgentBase<NaiveTestGAgentState, NaiveTestStateLogEvent, EventBase, NaiveGAgentConfiguration>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a naive test GAgent");
    }

    protected override async Task PerformConfigAsync(NaiveGAgentConfiguration configuration)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(configuration.Greeting);
    }
}

[GenerateSerializer]
public class NaiveGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string Greeting { get; set; } = string.Empty;
}