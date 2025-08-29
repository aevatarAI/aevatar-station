using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Router.GEvents;
using Aevatar.GAgents.Workflow.Test.TestGEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Workflow.Test.TestGAgents;

[Description("blockchain agent")]
public class BlockChainGAgent : GAgentBase<BlockChainState, BlockChainStateLogEvent>, IBlockChainGAgent
{
    private readonly ILogger<BlockChainGAgent> _logger;
    
    public BlockChainGAgent(ILogger<BlockChainGAgent> logger)
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "BlockChain");
    }

    [EventHandler]
    public async Task GetCurrentPrice(GetBitcoinPriceGEvent eventData)
    {
        await Task.Delay(1000); // Simulate some work
        await PublishAsync(new RouteNextGEvent
        {
            ProcessResult = "Current Price is 100000 dollar"
        });
    }
}

public interface IBlockChainGAgent : IGAgent
{
    
}

[GenerateSerializer]
public class BlockChainState : StateBase
{
}

public class BlockChainStateLogEvent : StateLogEventBase<BlockChainStateLogEvent>
{
}