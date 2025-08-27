using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Permissions;
using Aevatar.Service;
using Aevatar.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[Route("api/subscription")]
public class SubscriptionController : AevatarController
{
    private readonly SubscriptionAppService _subscriptionAppService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        SubscriptionAppService subscriptionAppService, 
        ILogger<SubscriptionController> logger)
    {
        _subscriptionAppService = subscriptionAppService;
        _logger = logger;
    }

    [HttpGet("events/{guid}")]
    [Authorize(Policy = AevatarPermissions.EventManagement.View)] 
    public async Task<List<EventDescriptionDto>> GetAvailableEventsAsync(Guid guid)
    {
        _logger.LogInformation("Get Available Events, id: {id}", guid);   
        return await _subscriptionAppService.GetAvailableEventsAsync(guid);
    }

    [HttpPost]
    [Authorize(Policy = AevatarPermissions.SubscriptionManagent.CreateSubscription)] 
    public async Task<SubscriptionDto> SubscribeAsync([FromBody] CreateSubscriptionDto input)
    {
        return await _subscriptionAppService.SubscribeAsync(input);
    }

    [HttpDelete("{subscriptionId:guid}")]
    [Authorize(Policy = AevatarPermissions.SubscriptionManagent.CancelSubscription)] 
    public async Task CancelSubscriptionAsync(Guid subscriptionId)
    {
        await _subscriptionAppService.CancelSubscriptionAsync(subscriptionId);
    }

    [HttpGet("{subscriptionId:guid}")]
    [Authorize(Policy = AevatarPermissions.SubscriptionManagent.ViewSubscriptionStatus)] 
    public async Task<SubscriptionDto> GetSubscriptionStatusAsync(Guid subscriptionId)
    {
        return await _subscriptionAppService.GetSubscriptionAsync(subscriptionId);
    }
}
