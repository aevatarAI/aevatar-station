using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.SignalR;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Aevatar.Notification;

public sealed class Notification_Test : AevatarApplicationTestBase
{
    private readonly INotificationHandlerFactory _notificationHandlerFactory;
    private readonly Mock<ILogger<NotificationService>> _logger;
    private readonly Mock<INotificationRepository> _notificationRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IHubService _hubService;
    private readonly NotificationService _notificationService;
    private readonly Guid _creator = Guid.Parse("fb63293b-fdde-4730-b10a-e95c373797c2");
    private readonly Guid _receiveId = Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703");
    private readonly NotificationInfo _notificationInfo;
    private readonly CancellationToken _cancellation;

    public Notification_Test()
    {
        _notificationHandlerFactory = GetRequiredService<INotificationHandlerFactory>();
        _logger = new Mock<ILogger<NotificationService>>();
        _notificationRepository = new Mock<INotificationRepository>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _hubService = GetRequiredService<IHubService>();
        _cancellation = new CancellationToken();

        _notificationService = new NotificationService(_notificationHandlerFactory, _logger.Object,
            _notificationRepository.Object, _objectMapper, _hubService);

        _notificationInfo = new NotificationInfo()
        {
            Type = NotificationTypeEnum.OrganizationInvitation,
            Input = new Dictionary<string, object>(),
            Content = "",
            Receiver = _receiveId,
            Status = NotificationStatusEnum.None,
            CreationTime = DateTime.Now,
            CreatorId = _creator,
        };
    }

    [Fact]
    public async Task CreatNotification_Test()
    {
        _notificationRepository.Setup(s => s.InsertAsync(_notificationInfo, false, _cancellation));
        
    }
}