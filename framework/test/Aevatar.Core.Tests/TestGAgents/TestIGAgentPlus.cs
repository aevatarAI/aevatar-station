using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;

namespace Aevatar.Core.Tests.TestGAgents;

public interface ITestIGAgentPlus : IGAgentPlus
{
    Task<string> GetTestValueAsync();
}

public class TestIGAgentPlus : GAgentBasePlus<TestStatePlus, TestStateLogEventPlus, EventBase, ConfigurationBase>, ITestIGAgentPlus
{
    public async Task<string> GetTestValueAsync()
    {
        return "IGAgentPlus-Test-Value";
    }

    public override async Task<string> GetDescriptionAsync()
    {
        return "Test IGAgentPlus Implementation";
    }
}

[GenerateSerializer] 
public class TestStatePlus : StateBasePlus
{
    [Id(0)] public string TestValue { get; set; } = "IGAgentPlus-State";
}

public class TestStateLogEventPlus : StateLogEventBase<TestStateLogEventPlus>
{
    [Id(0)] public string Action { get; set; } = "";
}
