using System.ComponentModel;
using Aevatar.Application.Grains.Agents.Investment.Dtos;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Investment;

[Description("Investment department,")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent("InvestmentGAgent")]
public class InvestmentGAgent : GAgentBase<InvestmentAgentState, InvestmentLogEvent, EventBase, InvestmentInitializeDto>, IInvestmentStateGAgent
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
    
    public override async Task InitializeAsync(InvestmentInitializeDto initializeDto)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(initializeDto.InvestmentContent);
        State.Number = initializeDto.Number;
    }

    [EventHandler]
    public async Task HandleEventAsync(InvestmentEvent eventData)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }
        Logger.LogInformation("handle InvestmentEvent, state: {content}, number: {number}", State.Content, State.Number);

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