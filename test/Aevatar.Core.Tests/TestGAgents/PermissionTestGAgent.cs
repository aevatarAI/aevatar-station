using Aevatar.Core.Abstractions;
using Aevatar.PermissionManagement;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

[GenerateSerializer]
public class PermissionGAgentState : StateBase
{

}

[GenerateSerializer]
public class PermissionStateLogEvent : StateLogEventBase<PermissionStateLogEvent>
{

}

public interface IPermissionGAgent : IGAgent
{
    Task DoSomethingAsync();
}

[GAgent]
public class PermissionGAgent : GAgentBase<PermissionGAgentState, PermissionStateLogEvent>, IPermissionGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for testing permissions.");
    }

    [Permission("DoSomething", "Only for testing.")]
    public Task DoSomethingAsync()
    {
        Logger.LogInformation("DoSomethingAsync called.");
        return Task.CompletedTask;
    }
}