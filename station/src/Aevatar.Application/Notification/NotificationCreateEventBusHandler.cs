using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;

namespace Aevatar.Notification;

public class NotificationCreateEventBusHandler:IDistributedEventHandler<NotificationCreatForEventBusDto>, ITransientDependency
{
    private readonly INotificationService _notificationService;

    public NotificationCreateEventBusHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task HandleEventAsync(NotificationCreatForEventBusDto eventData)
    {
        await _notificationService.CreateAsync(eventData.Type, eventData.Creator, eventData.Target, eventData.Content);
    }
}