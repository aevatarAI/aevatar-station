using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Service;
using Aevatar.Subscription;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Subscription")]
[Route("api/subscription")]
[Authorize]
public class SubscriptionController :  AevatarController
{
    private readonly SubscriptionAppService _subscriptionAppService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        SubscriptionAppService subscriptionAppService, 
        ILogger<SubscriptionController> logger )
    {
        _subscriptionAppService = subscriptionAppService;
        _logger = logger;
    }

    [HttpGet("events/{guid}")]
    public async Task<List<EventDescriptionDto>> GetAvailableEventsAsync(Guid guid)
    {
        _logger.LogInformation("Get Available Events, id: {id}", guid);   
        return await _subscriptionAppService.GetAvailableEventsAsync(guid);
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



