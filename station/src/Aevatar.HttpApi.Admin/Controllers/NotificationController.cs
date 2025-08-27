using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Notification;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Identity;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Notification")]
[Route("api/notification")]
[Authorize]
public class NotificationController : AevatarController
{
    private readonly INotificationService _notificationService;
    private readonly IdentityUserManager _userManager;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, IdentityUserManager userManager,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost]
    public async Task CreateApiKey([FromBody] CreateNotificationDto createDto)
    {
        if (createDto.Target == Guid.Empty && createDto.TargetEmail.IsNullOrEmpty())
        {
            _logger.LogError(
                $"[NotificationService][CreateAsync] creator error notificationTypeEnum:{createDto.Type.ToString()} , input:{createDto.Content}");
            throw new UserFriendlyException("target member error");
        }

        var target = createDto.Target;
        if (createDto.Target == Guid.Empty && createDto.TargetEmail.IsNullOrEmpty() == false)
        {
            var targetUserInfo = await _userManager.FindByEmailAsync(createDto.TargetEmail);
            if (targetUserInfo == null)
            {
                _logger.LogError(
                    $"[NotificationService][CreateAsync] creator email not found create email:{createDto.TargetEmail} , input:{createDto.Content}");
                throw new UserFriendlyException("creator not found");
            }

            target = targetUserInfo.Id;
        }

        if (CurrentUser.Id == target)
        {
            _logger.LogError(
                $"[NotificationService][CreateAsync]  Creator == target notificationTypeEnum:{createDto.Type.ToString()} targetMember:{target}, input:{createDto.Content}");
            throw new UserFriendlyException("Creator equal target");
        }

        // check target exist
        await _userManager.GetByIdAsync(target);

        await _notificationService.CreateAsync(createDto.Type, CurrentUser.Id, target, createDto.Content);
    }

    [HttpPost("withdraw/{guid}")]
    public async Task Withdraw(Guid guid)
    {
        if (await _notificationService.WithdrawAsync(CurrentUser.Id, guid))
        {
            throw new UserFriendlyException("handle withdraw fail");
        }
    }

    [HttpPost("response")]
    public async Task Response([FromBody] NotificationResponseDto responseDto)
    {
        if (await _notificationService.Response(responseDto.Id, CurrentUser.Id, responseDto.Status) == false)
        {
            throw new UserFriendlyException("handle notification fail");
        }
    }

    [HttpGet]
    public async Task<List<NotificationDto>> GetList(int pageIndex, int pageSize)
    {
        return await _notificationService.GetNotificationList(CurrentUser.Id, pageIndex, pageSize);
    }

    [HttpGet("organization")]
    public async Task<List<OrganizationVisitDto>> GetOrganizationVisitInfo(int pageIndex, int pageSize)
    {
        return await _notificationService.GetOrganizationVisitInfo((Guid)CurrentUser.Id!, pageIndex, pageSize);
    }
}