using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
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
    private readonly IPermissionManager _permissionManager;

    public AccountGAgent(IPermissionManager permissionManager)
    {
        _permissionManager = permissionManager;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for");
    }

    public async Task PublishEventAsync<T>(T @event) where T : EventBase
    {
        var userId = this.GetPrimaryKey();
        var currentUser = new UserContext
        {
            UserId = userId,
            Role = "Admin"
        };

        RequestContext.Set("CurrentUser", currentUser);
        
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        Logger.LogInformation($"AccountGAgent of {userId} publish {@event}");
        await PublishAsync(@event);
    }
}