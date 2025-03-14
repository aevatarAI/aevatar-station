using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

namespace Aevatar.Notification;

public interface INotificationService
{
    Task<bool> CreateAsync(NotificationTypeEnum notificationTypeEnum, Guid target, string? targetEmail, string input);
    Task<bool> WithdrawAsync(Guid notificationId);
    Task<bool> Response(Guid notificationId, NotificationStatusEnum status);
    Task<List<NotificationDto>> GetNotificationList(int pageIndex, int pageSize);
}

public class NotificationService : AevatarAppService, INotificationService
{
    private readonly INotificationHandlerFactory _notificationHandlerFactory;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationRepository _notificationRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IObjectMapper _objectMapper;

    public NotificationService(INotificationHandlerFactory notificationHandlerFactory,
        ILogger<NotificationService> logger, INotificationRepository notificationRepository,
        IdentityUserManager userManager, IObjectMapper objectMapper)
    {
        _notificationHandlerFactory = notificationHandlerFactory;
        _logger = logger;
        _notificationRepository = notificationRepository;
        _userManager = userManager;
        _objectMapper = objectMapper;
    }

    public async Task<bool> CreateAsync(NotificationTypeEnum notificationTypeEnum, Guid target, string? targetEmail,
        string input)
    {
        if (target == Guid.Empty && targetEmail.IsNullOrEmpty())
        {
            _logger.LogError(
                $"[NotificationService][CreateAsync] creator error notificationTypeEnum:{notificationTypeEnum.ToString()} , input:{input}");
            throw new ArgumentException("target member error");
        }

        if (target == Guid.Empty && targetEmail.IsNullOrEmpty() == false)
        {
            var targetUserInfo = await _userManager.FindByEmailAsync(targetEmail);
            if (targetUserInfo == null)
            {
                _logger.LogError(
                    $"[NotificationService][CreateAsync] creator email not found create email:{targetEmail} , input:{input}");
                throw new ArgumentException("creator not found");
            }

            target = targetUserInfo.Id;
        }

        _logger.LogDebug(
            $"[NotificationService][CreateAsync] notificationTypeEnum:{notificationTypeEnum.ToString()} targetMember:{target}, input:{input}");
        if (CurrentUser.Id == target)
        {
            _logger.LogError(
                $"[NotificationService][CreateAsync]  Creator == target notificationTypeEnum:{notificationTypeEnum.ToString()} targetMember:{target}, input:{input}");
            throw new ArgumentException("Creator equal target");
        }

        var notificationWrapper = _notificationHandlerFactory.GetNotification(notificationTypeEnum);
        if (notificationWrapper == null)
        {
            throw new BusinessException("Not found notification handler");
        }

        if (await notificationWrapper.CheckAuthorizationAsync(input, CurrentUser.Id!) == false)
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

        await _userManager.GetByIdAsync(target);

        var notification = new NotificationInfo()
        {
            Type = notificationTypeEnum,
            Input = JsonConvert.DeserializeObject<Dictionary<string, object>>(input)!,
            Content = content,
            Receiver = target,
            Status = NotificationStatusEnum.None,
            CreationTime = DateTime.Now,
            CreatorId = CurrentUser.Id,
        };

        await _notificationRepository.InsertAsync(notification);

        return true;
    }

    public async Task<bool> WithdrawAsync(Guid notificationId)
    {
        var notification = await _notificationRepository.GetAsync(notificationId);
        if (notification.CreatorId != CurrentUser.Id || notification.Status != NotificationStatusEnum.None)
        {
            return false;
        }

        // todo: update Transaction
        notification.Status = NotificationStatusEnum.Withdraw;
        await _notificationRepository.UpdateAsync(notification);
        return true;
    }

    public async Task<bool> Response(Guid notificationId, NotificationStatusEnum status)
    {
        var notification = await _notificationRepository.GetAsync(notificationId);
        if (notification.Receiver != CurrentUser.Id)
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
        return true;
    }

    public async Task<List<NotificationDto>> GetNotificationList(int pageIndex, int pageSize)
    {
        var query = await _notificationRepository.GetQueryableAsync();
        var queryResponse = query.Where(w => w.Receiver == CurrentUser.Id || w.CreatorId == CurrentUser.Id)
            .OrderByDescending(o => o.CreationTime).Skip(pageSize * pageIndex).Take(pageSize).ToList();

        return _objectMapper.Map<List<NotificationInfo>, List<NotificationDto>>(queryResponse);
    }
}