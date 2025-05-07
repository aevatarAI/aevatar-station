using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Domain.Grains.Subscription;
using Orleans;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgent;

public class SubscriptionGAgentTests : AevatarApplicationTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ITestOutputHelper _output;
    public SubscriptionGAgentTests(ITestOutputHelper output)
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _output = output;
    }

    [Fact]
    public async Task AddSubscriptionTest()
    {
      var eventSubscription =  await _clusterClient.GetGrain<ISubscriptionGAgent>(Guid.NewGuid()).SubscribeAsync(
            new SubscribeEventInputDto
            {
                AgentId = Guid.NewGuid(),
                EventTypes = new List<string>()
                {
                    "Created", "Updated"
                },
                CallbackUrl = "http://127.0.0.1"
            });
      eventSubscription.AgentId.ShouldNotBe(Guid.Empty);
      eventSubscription.CallbackUrl.ShouldNotBeNullOrEmpty();
      eventSubscription.Status.ShouldBe("Active");
    }
    
    [Fact]
    public async Task CancelSubscriptionTest()
    {
        var subscriptionId = Guid.NewGuid();
        await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId).UnsubscribeAsync();
        var subscription = await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId).GetStateAsync();
        subscription.Status.ShouldBeNullOrEmpty();
        await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId).SubscribeAsync(
            new SubscribeEventInputDto
            {
                AgentId = Guid.NewGuid(),
                EventTypes = new List<string>()
                {
                    "Created", "Updated"
                },
                CallbackUrl = "http://127.0.0.1"
            });
        await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId).UnsubscribeAsync();
        subscription = await _clusterClient.GetGrain<ISubscriptionGAgent>(subscriptionId).GetStateAsync();
        subscription.Status.ShouldBe("Cancelled");
    }
    
   

}