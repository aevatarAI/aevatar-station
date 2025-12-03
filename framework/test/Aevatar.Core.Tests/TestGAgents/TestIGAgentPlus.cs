using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;

namespace Aevatar.Core.Tests.TestGAgents;

public interface ITestIGAgentPlus : IGAgentPlus
{
    Task<string> GetTestValueAsync();
    Task<Dictionary<string, Guid>> GetSubscriptionHandlesAsync();
}

[GrainType("Aevatar.Core.Tests.TestGAgents.TestIGAgentPlus")]
public class TestIGAgentPlus : GAgentBasePlus<TestStatePlus, TestStateLogEventPlus, EventBase, ConfigurationBase>, ITestIGAgentPlus
{
    public async Task<string> GetTestValueAsync()
    {
        return "IGAgentPlus-Test-Value";
    }

    public async Task<Dictionary<string, Guid>> GetSubscriptionHandlesAsync()
    {
        // Return a copy of the subscription handles from the internal state
        return new Dictionary<string, Guid>(State.Subscription ?? new Dictionary<string, Guid>());
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
