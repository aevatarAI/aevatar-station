using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Brain;
using Aevatar.AI.BrainFactory;
using Aevatar.AI.Dtos;
using Aevatar.AI.Events;
using Aevatar.AI.State;
using Aevatar.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.AI.Agent;

public abstract class AIGAgentBase<TState, TEvent> : GAgentBase<TState, TEvent>, IAIGAgent
    where TState : AIGAgentState, new()
    where TEvent : AIEventBase
{
    private readonly IBrainFactory _brainFactory;
    private IBrain? _brain = null;
    
    public AIGAgentBase(ILogger logger) : base(logger)
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }

    public Task<bool> InitializeAsync(InitializeDto initializeDto)
    {
        //save state

        _brain = _brainFactory.GetBrain(initializeDto.LLM);
        
        if(_brain == null)
        {
            Logger.LogError("Failed to initialize brain.");
            return Task.FromResult(false);
        }
        
        var result = _brain.Initialize(
            Guid.Parse(this.GetGrainId().ToString()),
            initializeDto.Instructions, 
            initializeDto.Files.Select(f => new File(){Content = f.Content, Type = f.Type}).ToList());
        
        return Task.FromResult(result);
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
    }

    protected async Task<string?> InvokePromptAsync(string prompt)
    {
        return await _brain?.InvokePromptAsync(prompt)!;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}