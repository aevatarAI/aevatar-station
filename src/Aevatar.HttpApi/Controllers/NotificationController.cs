using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Notification;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Notification")]
[Route("api/notification")]
[Authorize]
public class NotificationController
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    [HttpPost]
    public async Task CreateApiKey([FromBody] CreateNotificationDto createDto)
    {
        await _notificationService.CreateAsync(createDto.Type, createDto.Target, createDto.Content);
    }
    
    [HttpPost("/withdraw/{guid}")]
    public async Task Withdraw(Guid guid)
    {
        await _notificationService.WithdrawAsync(guid);
    }

    [HttpPost("/response")]
    public async Task Response([FromBody]NotificationResponseDto responseDto)
    {
        await _notificationService.Response(responseDto.Id, responseDto.Status);
    }
    
    [HttpGet]
    public async Task<List<NotificationDto>> GetList(int pageIndex, int pageSize)
    {
        return await _notificationService.GetNotificationList(pageIndex,pageSize);
    }
}