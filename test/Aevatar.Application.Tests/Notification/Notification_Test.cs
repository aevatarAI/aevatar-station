using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.SignalR;
using Aevatar.SignalR.SignalRMessage;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Aevatar.Notification;

public sealed class Notification_Test : AevatarApplicationTestBase
{
    private readonly INotificationHandlerFactory _notificationHandlerFactory;
    private readonly Mock<ILogger<NotificationService>> _logger;
    private readonly Mock<INotificationRepository> _notificationRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly Mock<IHubService> _hubService;
    private readonly NotificationService _notificationService;
    private readonly NotificationStatusEnum _notificationStatusEnum = NotificationStatusEnum.Agree;
    private readonly Guid _creator = Guid.Parse("fb63293b-fdde-4730-b10a-e95c373797c2");
    private readonly Guid _receiveId = Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703");
    private readonly Guid _notificationId = Guid.Parse("1263293b-fdde-4730-b10a-e95c37379743");
    private readonly NotificationInfo _notificationInfo;
    private readonly CancellationToken _cancellation;
    private readonly string _input = "{\"OrganizationId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\", \"Role\":1}";

    public Notification_Test()
    {
        _notificationHandlerFactory = GetRequiredService<INotificationHandlerFactory>();
        _logger = new Mock<ILogger<NotificationService>>();
        _notificationRepository = new Mock<INotificationRepository>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _hubService = new Mock<IHubService>();
        _cancellation = new CancellationToken();

        _notificationService = new NotificationService(_notificationHandlerFactory, _logger.Object,
            _notificationRepository.Object, _objectMapper, _hubService.Object);

        _hubService.Setup(f => f.ResponseAsync(_receiveId,
                new NotificationResponse() { Data = { Id = _notificationId, status = _notificationStatusEnum } }))
            .Returns(Task.CompletedTask);

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
        var response = await _notificationService.CreateAsync(NotificationTypeEnum.OrganizationInvitation, _creator,
            _receiveId, _input);
        response.ShouldBeTrue();
    }
}