using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Aevatar.Notification.Parameters;
using Aevatar.Organizations;
using Aevatar.SignalR;
using Aevatar.SignalR.SignalRMessage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace Aevatar.Notification;

[RemoteService(false)]
public class NotificationService : INotificationService, ITransientDependency
{
    private readonly INotificationHandlerFactory _notificationHandlerFactory;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationRepository _notificationRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IHubService _hubService;
    private readonly IOrganizationService _organizationService;

    public NotificationService(INotificationHandlerFactory notificationHandlerFactory,
        ILogger<NotificationService> logger, INotificationRepository notificationRepository, IObjectMapper objectMapper,
        IHubService hubService, IOrganizationService organizationService)
    {
        _notificationHandlerFactory = notificationHandlerFactory;
        _logger = logger;
        _notificationRepository = notificationRepository;
        _objectMapper = objectMapper;
        _hubService = hubService;
        _organizationService = organizationService;
    }

    public async Task<bool> CreateAsync(NotificationTypeEnum notificationTypeEnum, Guid? creator, Guid target,
        string input)
    {
        _logger.LogDebug(
            $"[NotificationService][CreateAsync] notificationTypeEnum:{notificationTypeEnum.ToString()} targetMember:{target}, input:{input}");

        var notificationWrapper = _notificationHandlerFactory.GetNotification(notificationTypeEnum);
        if (notificationWrapper == null)
        {
            throw new BusinessException("Not found notification handler");
        }

        if (await notificationWrapper.CheckAuthorizationAsync(input, creator) == false)
        {
            throw new AuthenticationException("Permission Denied or Insufficient Permissions.");
        }

        var parameter = notificationWrapper.ConvertInput(input);
        if (parameter == null)
        {
            throw new ArgumentException("Argument Error");
        }

        var content = await notificationWrapper.GetNotificationMessage(parameter);
        if (content == null)
        {
            throw new ArgumentException("Argument Error");
        }

        var notification = new NotificationInfo()
        {
            Type = notificationTypeEnum,
            Input = JsonConvert.DeserializeObject<Dictionary<string, object>>(input)!,
            Content = content,
            Receiver = target,
            Status = NotificationStatusEnum.None,
            CreationTime = DateTime.Now,
            CreatorId = creator,
        };

        notification = await _notificationRepository.InsertAsync(notification);
        await _hubService.ResponseAsync([(Guid)notification.CreatorId!, notification.Receiver],
            new NotificationResponse()
            {
                Data = new NotificationResponseMessage()
                    { Id = notification.Id, Status = NotificationStatusEnum.None }
            });
        return true;
    }

    public async Task<bool> WithdrawAsync(Guid? creator, Guid notificationId)
    {
        var notification = await _notificationRepository.GetAsync(notificationId);
        if (notification.CreatorId != creator || notification.Status != NotificationStatusEnum.None)
        {
            return false;
        }

        // todo: update Transaction
        notification.Status = NotificationStatusEnum.Withdraw;
        await _notificationRepository.UpdateAsync(notification);

        await _hubService.ResponseAsync([(Guid)notification.CreatorId!, notification.Receiver],
            new NotificationResponse()
            {
                Data = new NotificationResponseMessage()
                    { Id = notificationId, Status = NotificationStatusEnum.Withdraw }
            });

        return true;
    }

    public async Task<bool> Response(Guid notificationId, Guid? receiver, NotificationStatusEnum status)
    {
        var notification = await _notificationRepository.GetAsync(notificationId);
        if (notification.Receiver != receiver)
        {
            _logger.LogError(
                $"[NotificationService][Response] notification.Receiver != CurrentUser.Id notificationId:{notificationId}");
            return false;
        }

        if (notification.Status != NotificationStatusEnum.None || status == NotificationStatusEnum.None)
        {
            _logger.LogError(
                $"[NotificationService][Response] notification.Status != NotificationStatusEnum.None notificationId:{notificationId}");
            return false;
        }

        var notificationWrapper = _notificationHandlerFactory.GetNotification(notification.Type);
        if (notificationWrapper == null)
        {
            return false;
        }

        // do business logic
        await notificationWrapper.ProcessNotificationAsync(notification.Input, status);
        notification.Status = status;

        await _notificationRepository.UpdateAsync(notification);

        await _hubService.ResponseAsync([(Guid)notification.CreatorId!, notification.Receiver],
            new NotificationResponse()
                { Data = new NotificationResponseMessage() { Id = notificationId, Status = status } });
        return true;
    }

    public async Task<List<NotificationDto>> GetNotificationList(Guid? creator, int pageIndex, int pageSize)
    {
        var query = await _notificationRepository.GetQueryableAsync();
        var queryResponse = query.Where(w => w.Receiver == creator || w.CreatorId == creator)
            .OrderByDescending(o => o.CreationTime).Skip(pageSize * pageIndex).Take(pageSize).ToList();

        return _objectMapper.Map<List<NotificationInfo>, List<NotificationDto>>(queryResponse);
    }

    public async Task<List<OrganizationVisitDto>> GetOrganizationVisitInfo(Guid userId, int pageIndex, int pageSize)
    {
        var query = await _notificationRepository.GetQueryableAsync();
        var queryResponse = query.Where(w =>
            w.Receiver == userId && w.Status == NotificationStatusEnum.None &&
            w.Type == NotificationTypeEnum.OrganizationInvitation).Skip(pageSize * pageIndex).Take(pageSize).ToList();

        var result = new List<OrganizationVisitDto>();
        if (queryResponse.Count == 0)
        {
            return result;
        }

        var notificationWrapper =
            _notificationHandlerFactory.GetNotification(NotificationTypeEnum.OrganizationInvitation);
        if (notificationWrapper == null)
        {
            return result;
        }

        foreach (var item in queryResponse)
        {
            var organizationInfoObj = notificationWrapper.ConvertInput(JsonConvert.SerializeObject(item.Input));
            if (organizationInfoObj != null)
            {
                var organizationVisitInfo = organizationInfoObj as OrganizationVisitInfo;
                var  organizationInfo = await _organizationService.GetAsync(organizationVisitInfo!.OrganizationId);
                result.Add(new OrganizationVisitDto()
                {
                    Id = item.Id,
                    OrganizationId = organizationInfo.Id,
                    OrganizationName = organizationInfo.DisplayName,
                });
            }
        }

        return result;
    }
}