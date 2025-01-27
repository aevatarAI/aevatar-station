using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Service;
using Aevatar.Subscription;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Subscription")]
[Route("api/subscription")]
[Authorize]
public class SubscriptionController :  AevatarController
{
    private readonly SubscriptionAppService _subscriptionAppService;

    public SubscriptionController(SubscriptionAppService subscriptionAppService)
    {
        _subscriptionAppService = subscriptionAppService;
    }

    [HttpGet("events")]
    public async Task<List<EventDescriptionDto>> GetAvailableEventsAsync([FromQuery] Guid agentId)
    {
        return await _subscriptionAppService.GetAvailableEventsAsync(agentId);
    }

    [HttpPost]
    public async Task<SubscriptionDto> SubscribeAsync([FromBody] CreateSubscriptionDto input)
    {
        return await _subscriptionAppService.SubscribeAsync(input);
    }

    [HttpDelete("{subscriptionId:guid}")]
    public async Task CancelSubscriptionAsync(Guid subscriptionId)
    {
        await _subscriptionAppService.CancelSubscriptionAsync(subscriptionId);
    }

    [HttpGet("{subscriptionId:guid}")]
    public async Task<SubscriptionDto> GetSubscriptionStatusAsync(Guid subscriptionId)
    {
        return await _subscriptionAppService.GetSubscriptionAsync(subscriptionId);
    }
    
    [HttpPost("publish")]
    public async Task PublishAsync([FromBody] PublishEventDto input)
    {
        await _subscriptionAppService.PublishEventAsync(input);
    }
}



