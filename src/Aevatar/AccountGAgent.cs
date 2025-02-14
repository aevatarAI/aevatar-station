using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar;

[GenerateSerializer]

public class AccountGAgentState : StateBase;

[GenerateSerializer]
public class AccountStateLogEvent : StateLogEventBase<AccountStateLogEvent>;

[GAgent]
public class AccountGAgent : GAgentBase<AccountGAgentState, AccountStateLogEvent>, IAccountGAgent
{
    private readonly IPermissionChecker _permissionChecker;

    public AccountGAgent(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for publishing events on behalf of users.");
    }

    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        await PublishAsync(@event);
    }
}