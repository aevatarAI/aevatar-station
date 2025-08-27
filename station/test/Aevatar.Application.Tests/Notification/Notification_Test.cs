using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Organizations;
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
    private static readonly NotificationStatusEnum _notificationStatusEnum = NotificationStatusEnum.Agree;
    private readonly Guid _creator = Guid.Parse("fb63293b-fdde-4730-b10a-e95c373797c2");
    private readonly Guid _receiveId = Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703");
    private static readonly Guid _notificationId = Guid.Parse("1263293b-fdde-4730-b10a-e95c37379743");
    private readonly NotificationInfo _notificationInfo;
    private readonly CancellationToken _cancellation;
    private readonly Mock<IOrganizationService> _organizationService;
    private readonly string _input = "{\"OrganizationId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\", \"Role\":1}";

    private readonly NotificationResponse _notificationResponse = new NotificationResponse()
        { Data = new NotificationResponseMessage() { Id = _notificationId, Status = _notificationStatusEnum } };

    public Notification_Test()
    {
        _notificationHandlerFactory = GetRequiredService<INotificationHandlerFactory>();
        _logger = new Mock<ILogger<NotificationService>>();
        _notificationRepository = new Mock<INotificationRepository>();
        _organizationService = new Mock<IOrganizationService>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _hubService = new Mock<IHubService>();
        _cancellation = new CancellationToken();

        _notificationService = new NotificationService(_notificationHandlerFactory, _logger.Object,
            _notificationRepository.Object, _objectMapper, _hubService.Object, _organizationService.Object);

        _hubService.Setup(f => f.ResponseAsync(new List<Guid>(){_receiveId}, _notificationResponse))
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

    [Fact]
    public async Task WithdrawAsync_Test()
    {
        _notificationRepository.Setup(s => s.GetAsync(_notificationId, true, _cancellation))
            .ReturnsAsync(_notificationInfo);
        _notificationRepository.Setup(s => s.UpdateAsync(_notificationInfo, false, _cancellation))
            .ReturnsAsync(_notificationInfo);

        var response = await _notificationService.WithdrawAsync(_creator, _notificationId);

        response.ShouldBeTrue();
    }

    [Fact]
    public async Task ResponseAsync_Test()
    {
        _notificationRepository.Setup(s => s.GetAsync(_notificationId, true, _cancellation))
            .ReturnsAsync(_notificationInfo);
        _notificationRepository.Setup(s => s.UpdateAsync(_notificationInfo, false, _cancellation))
            .ReturnsAsync(_notificationInfo);

        var response = await _notificationService.Response(_notificationId, _receiveId, NotificationStatusEnum.Agree);

        response.ShouldBeTrue();
    }
}