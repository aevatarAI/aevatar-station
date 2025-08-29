using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Router.GAgents;
using Aevatar.GAgents.Router.GEvents;
using Aevatar.GAgents.Workflow.Test.TestGAgents;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Workflow.Test;

public class RouterGAgentTest : AevatarWorkflowTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public RouterGAgentTest(ITestOutputHelper outputHelper)
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task RouterTest()
    {
        var routerGAgent = await _gAgentFactory.GetGAgentAsync<IRouterGAgent>(Guid.NewGuid());
        await routerGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a router agent",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });


        var blockChainGAgent = await _gAgentFactory.GetGAgentAsync<IBlockChainGAgent>(Guid.NewGuid());
        var blockChainGAgentEvents = await blockChainGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(blockChainGAgent.GetType(), blockChainGAgentEvents);

        var twitterGAgent = await _gAgentFactory.GetGAgentAsync<ITwitterGAgent>(Guid.NewGuid());
        var twitterGAgentEvents = await twitterGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(twitterGAgent.GetType(), twitterGAgentEvents);

        var groupGAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        await groupGAgent.RegisterAsync(blockChainGAgent);
        await groupGAgent.RegisterAsync(routerGAgent);
        await groupGAgent.RegisterAsync(twitterGAgent);

        var state = await routerGAgent.GetStateAsync();
        await groupGAgent.PublishEventAsync(new BeginTaskGEvent()
        {
            TaskDescription = "I want to post a tweet about the current price of bitcoin"
        });


        await Task.Delay(100000);
    }
}