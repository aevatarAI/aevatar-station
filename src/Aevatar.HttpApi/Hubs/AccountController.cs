using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Account;
using Aevatar.Notification;
using Aevatar.SignalR;
using Aevatar.SignalR.SignalRMessage;
using Asp.Versioning;
using Volo.Abp;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Account;
using Volo.Abp.Identity;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("TestHub")]
[Route("api/test-hub")]
public class TestHubController : AevatarController
{
    private readonly IHubService _hubService;

    public TestHubController(IHubService hubService)
    {
        _hubService = hubService;
    }
    
    [HttpPost]
    [Route("message/{userId}")]
    public virtual Task MessageAsync(Guid userId)
    {
        return _hubService.ResponseAsync(new List<Guid>{userId},new NotificationResponse()
        {
            Data = new NotificationResponseMessage()
                { Id = Guid.NewGuid(), Status = NotificationStatusEnum.None }
        });
    }
    
    [HttpPost]
    [Route("unread/{userId}")]
    public virtual Task UnreadAsync(Guid userId)
    {
        return _hubService.ResponseAsync([userId],
            new UnreadNotificationResponse()
                { Data = new UnreadNotification(unreadCount: 10) });
    }
}