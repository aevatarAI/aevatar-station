using Aevatar.Core.Abstractions;
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
    [Permission("DoSomething")]
    Task DoSomethingAsync();
}

[GAgent]
public class PermissionGAgent : GAgentBase<PermissionGAgentState, PermissionStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for testing permissions.");
    }
    
    
}