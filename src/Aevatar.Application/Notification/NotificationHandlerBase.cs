using System;
using System.Threading.Tasks;

namespace Aevatar.Notification;

public abstract class NotificationHandlerBase<T> : INotificationHandler<T> where T : class
{
    public abstract NotificationTypeEnum Type { get; }
    public abstract T ConvertInput(string input);

    public abstract Task<string> GetContentForShowAsync(T input);

    public virtual Task<bool> CheckAuthorizationAsync(T input, Guid creator)
    {
        return Task.FromResult(true);
    }

    public abstract Task HandleAgreeAsync(T input);

    public virtual Task HandleRefuseAsync(T input)
    {
        return Task.CompletedTask;
    }

    public virtual Task HandleReadAsync(T input)
    {
        return Task.CompletedTask;
    }

    public virtual Task HandleIgnoreAsync(T input)
    {
        return Task.CompletedTask;
    }

    public virtual Task HandleWithdrawAsync(T input)
    {
        return Task.CompletedTask;
    }
}