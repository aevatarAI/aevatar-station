using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.PermissionManagement;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.TestAgent;

[Description("AgentPermissionTest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AgentPermissionTest : PermissionGAgentBase<PermissionAgentState, PermissionTestEvent>
{
    private readonly ILogger<AgentPermissionTest> _logger;

    public AgentPermissionTest(ILogger<AgentPermissionTest> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a permission test agent");
    }
    
    [EventHandler]
    public async Task HandleEventAsync(SetAuthorizedUserEvent @event)
    {
        _logger.LogInformation("SetAuthorizedUserEvent: {UserId}", @event.UserId);
        await AddAuthorizedUsersAsync(@event.UserId);
    }
}

[GenerateSerializer]
public class PermissionAgentState : PermissionStateBase
{
}

[GenerateSerializer]
public class PermissionTestEvent : StateLogEventBase<PermissionTestEvent>
{
}

[GenerateSerializer]
public class SetAuthorizedUserEvent : EventBase
{
    [Id(0)]
    public Guid UserId { get; set; }
} 