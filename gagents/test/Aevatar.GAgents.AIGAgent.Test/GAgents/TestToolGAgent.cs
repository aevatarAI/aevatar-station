using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents;

[GenerateSerializer]
public class TestToolGAgentState : StateBase
{
}

[GenerateSerializer]
public class TestToolGAgentStateLogEvent : StateLogEventBase<TestToolGAgentStateLogEvent>;

public interface ITestToolGAgent : IStateGAgent<TestToolGAgentState>
{
}

[GAgent("testtool", AevatarGAgentsConstants.ToolGAgentNamespace)]
public class TestToolGAgent : GAgentBase<TestToolGAgentState, TestToolGAgentStateLogEvent>, ITestToolGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test Tool GAgent for resource context tests");
    }
}

