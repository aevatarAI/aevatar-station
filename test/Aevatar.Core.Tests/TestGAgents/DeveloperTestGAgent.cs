using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

public interface IDeveloperTestGAgent: IGAgent
{
    
}

[GAgent("developer", "Test")]
public class DeveloperTestGAgent : GAgentBase<NaiveTestGAgentState, NaiveTestGEvent>, IDeveloperTestGAgent
{
    public DeveloperTestGAgent(ILogger<MarketingLeaderTestGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This GAgent acts as a developer.");
    }

    public async Task<NewFeatureCompletedTestEvent> HandleEventAsync(DevelopTaskTestEvent eventData)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(eventData.Description);

        return new NewFeatureCompletedTestEvent
        {
            PullRequestUrl = $"PR for {eventData.Description}"
        };
    }
}