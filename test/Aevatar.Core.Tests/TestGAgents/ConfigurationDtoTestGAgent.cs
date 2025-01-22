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

public class Configuration : ConfigurationBase
{
    [Id(0)] public string InitialGreeting { get; set; }
}

[GAgent("initialize")]
public class ConfigurationDtoTestGAgent : GAgentBase<InitializeDtoTestGAgentState,
    InitializeDtoTestStateLogEvent, EventBase, Configuration>
{
    public ConfigurationDtoTestGAgent(ILogger<ConfigurationDtoTestGAgent> logger) : base(logger)
    {
    }

    protected override Task PerformConfigAsync(Configuration configuration)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(configuration.InitialGreeting);
        return Task.CompletedTask;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a test GAgent for initialization testing.");
    }
}