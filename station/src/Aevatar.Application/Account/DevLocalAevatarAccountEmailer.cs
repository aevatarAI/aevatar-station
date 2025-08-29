using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace Aevatar.Account;

public class DevLocalAevatarAccountEmailer : IAevatarAccountEmailer, ITransientDependency
{
    private readonly ILogger<DevLocalAevatarAccountEmailer> _logger;

    public DevLocalAevatarAccountEmailer(ILogger<DevLocalAevatarAccountEmailer> logger)
    {
        _logger = logger;
    }

    public Task SendRegisterCodeAsync(string email, string code)
    {
        _logger.LogInformation("[LOCAL DEV] Registration email would be sent to: {Email}, Code: {Code}", 
            email, code);
        
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string resetToken)
    {
        _logger.LogInformation("[LOCAL DEV] Password reset email would be sent to: {Email} (UserId: {UserId}), ResetToken: {ResetToken}", 
            user.Email, user.Id, resetToken);
        
        return Task.CompletedTask;
    }
}
