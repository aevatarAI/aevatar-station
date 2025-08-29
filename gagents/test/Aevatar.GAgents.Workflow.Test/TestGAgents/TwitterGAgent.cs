using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Router.GEvents;
using Aevatar.GAgents.Workflow.Test.TestGEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Workflow.Test.TestGAgents;

[Description("An intelligent Twitter Agent capable of posting tweets, fetching information, and managing replies.")]
public class TwitterGAgent : GAgentBase<TwitterState, TwitterStateLogEvent>, ITwitterGAgent
{
    private readonly ILogger<TwitterGAgent> _logger;
    
    public TwitterGAgent(ILogger<TwitterGAgent> logger)
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "TwitterGAgent");
    }

    [EventHandler]
    public async Task HandleEventAsync(PostTweetGEvent @event)
    {
        _logger.LogDebug("HandleEventAsync CreateTweetEvent, text: {text}", @event.Text);
        if (@event.Text.IsNullOrEmpty())
        {
            return;
        }
        
        await Task.Delay(1000);
        
        var evt = await PublishAsync(new RouteNextGEvent
        {
            ProcessResult = "Post tweet success"
        });
    }
    
    [EventHandler]
    public async Task HandleEventAsync(QueryMentionGEvent @event)
    {
        await Task.Delay(1000);
        
        var evt = await PublishAsync(new RouteNextGEvent
        {
            ProcessResult = "Query content"
        });
    }
}

public interface ITwitterGAgent : IGAgent
{
    
}

[GenerateSerializer]
public class TwitterState : StateBase
{
}

public class TwitterStateLogEvent : StateLogEventBase<TwitterStateLogEvent>
{
}