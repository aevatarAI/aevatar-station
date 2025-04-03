using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Notification;

public interface INotificationHandlerType
{
    NotificationTypeEnum Type { get; }
}

public interface INotificationHandler<T> : INotificationHandlerType where T : class
{
    T ConvertInput(string input);
    Task<string> GetContentForShowAsync(T input);
    Task<bool> CheckAuthorizationAsync(T input, Guid creator);
    Task HandleAgreeAsync(T input);
    Task HandleRefuseAsync(T input);
    Task HandleReadAsync(T input);
    Task HandleIgnoreAsync(T input);
    Task HandleWithdrawAsync(T input);
}