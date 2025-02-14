using System.Reflection;
using System.Security.Claims;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Security.Claims;

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
        ArgumentNullException.ThrowIfNull(@event);

        var permissionAttribute = @event.GetType().GetCustomAttribute<PermissionAttribute>();

        if (permissionAttribute != null)
        {
            if (RequestContext.Get("CurrentUser") is not UserContext currentUser) return;

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity([
                    new Claim(AbpClaimTypes.UserId, currentUser.UserId.ToString()),
                    new Claim(AbpClaimTypes.Role, currentUser.Role)
                ], "Bearer"));

            if (claimsPrincipal.Identity is not { IsAuthenticated: true })
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var isGranted = await _permissionChecker.IsGrantedAsync(
                claimsPrincipal,
                permissionAttribute.Name
            );

            if (isGranted)
            {
                throw new UnauthorizedAccessException(
                    $"Required permission '{permissionAttribute.Name}' is not granted."
                );
            }
        }
        
        await PublishAsync(@event);
    }
}