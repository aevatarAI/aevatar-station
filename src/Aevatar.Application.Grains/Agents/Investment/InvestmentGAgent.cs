using Aevatar.Application.Grains.Agents.Investment.Dtos;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.Investment;

[GAgent("InvestmentGAgent")]
public class InvestmentGAgent : GAgentBase<InvestmentAgentState, InvestmentLogEvent, EventBase, InvestmentConfiguration>, IInvestmentStateGAgent
{
    public InvestmentGAgent(ILogger<InvestmentGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a social event is published.");
    }

    public Task<InvestmentAgentState> GetStateAsync()
    {
        return Task.FromResult(State);
    }
    
    
    protected override async Task PerformConfigAsync(InvestmentConfiguration configuration)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(configuration.InvestmentContent);
        State.Number = configuration.Number;
    }

    [EventHandler]
    public async Task HandleEventAsync(InvestmentEvent eventData)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(eventData.Content);
        await PublishAsync(new SendMessageEvent
        {
            Message = "InvestmentGAgent Completed."
        });
    }
    
}

public interface IInvestmentStateGAgent :  IStateGAgent<InvestmentAgentState>
{

}