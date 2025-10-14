using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;

namespace Aevatar.Core.Tests.TestGAgents;

public interface ITestIGAgent : IGAgent
{
    Task<string> GetTestValueAsync();
}

[GrainType("TestIGAgent")]
public class TestIGAgent : GAgentBase<TestState, TestStateLogEvent, EventBase, ConfigurationBase>, ITestIGAgent
{
    public async Task<string> GetTestValueAsync()
    {
        return "IGAgent-Test-Value";
    }

    public override async Task<string> GetDescriptionAsync()
    {
        return "Test IGAgent Implementation";
    }
}

[GenerateSerializer]
public class TestState : StateBase
{
    [Id(0)] public string TestValue { get; set; } = "IGAgent-State";
}

public class TestStateLogEvent : StateLogEventBase<TestStateLogEvent>
{
    [Id(0)] public string Action { get; set; } = "";
}
