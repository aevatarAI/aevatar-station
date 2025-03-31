using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.GAgents.AIGAgent.State;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace Aevatar.StateProjector;

public class AevatarTokenUsageProjector : IStateProjector, ISingletonDependency
{
    private readonly ConcurrentQueue<TokenUsage?> _queue = new ConcurrentQueue<TokenUsage?>();
    private readonly  AutoResetEvent _dataAvailable = new AutoResetEvent(false);
    private readonly IMediator _mediator;   
    private readonly ILogger<AevatarStateProjector> _logger;

    public AevatarTokenUsageProjector(IMediator mediator, ILogger<AevatarStateProjector> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        dynamic wrapper = state;
        GrainId grainId = wrapper.GrainId;
        StateBase wrapperState = wrapper.State;
        int version = wrapper.Version;

        var aiGAgentState = wrapperState as AIGAgentStateBase;
        if (aiGAgentState == null || aiGAgentState.LastTotalTokenUsage == 0)
        {
            return Task.CompletedTask;
        }

        _logger.LogDebug($"[AevatarTokenUsageProjector] ProjectAsync get message:{JsonConvert.SerializeObject(aiGAgentState)}");
        var saveTokenCommand = new TokenUsage
        {
            GrainId = grainId.ToString(),
            SystemLLMConfig = aiGAgentState.SystemLLM.IsNullOrEmpty() ? "" : aiGAgentState.SystemLLM,
            IfUserLLMProvider = aiGAgentState.SystemLLM.IsNullOrEmpty(),
            LastTotalTokenUsage = aiGAgentState.LastInputTokenUsage,
            LastInputTokenUsage = aiGAgentState.LastInputTokenUsage,
            LastOutTokenUsage = aiGAgentState.LastOutTokenUsage,
            TotalTokenUsage = aiGAgentState.TotalTokenUsage,
            InputTokenUsage = aiGAgentState.InputTokenUsage,
            OutTokenUsage = aiGAgentState.OutTokenUsage
        };
        
        _queue.Enqueue(saveTokenCommand);
        _dataAvailable.Set();
        
        return Task.CompletedTask;
    }

    private async Task ProcessCommand()
    {
        while (true)
        {
            _dataAvailable.WaitOne();

            while (true)
            {
                var listGrain = new List<TokenUsage>();
                while (_queue.TryDequeue(out TokenUsage? message))
                {
                    if (message != null)
                    {
                        listGrain.Add(message);
                    }
                }

                if (listGrain.Count == 0)
                {
                    break;
                }
                
                _logger.LogDebug($"[AevatarTokenUsageProjector][ProcessCommand] save token count:{listGrain.Count}");
                await _mediator.Send(new TokenUsageCommand() { TokenUsages = listGrain});
            }
        }
        
        // ReSharper disable once FunctionNeverReturns
    }
}