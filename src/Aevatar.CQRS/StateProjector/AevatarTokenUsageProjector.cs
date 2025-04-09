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
    private readonly ConcurrentQueue<CQRS.Dto.TokenUsage?> _queue = new ConcurrentQueue<CQRS.Dto.TokenUsage?>();
    private readonly AutoResetEvent _dataAvailable = new AutoResetEvent(false);

    private readonly ConcurrentDictionary<GrainId, Tuple<int, DateTime>> _versionToTime =
        new ConcurrentDictionary<GrainId, Tuple<int, DateTime>>();

    private readonly IMediator _mediator;
    private DateTime _lastUpdateTime = DateTime.Now;
    private readonly ILogger<AevatarTokenUsageProjector> _logger;

    public AevatarTokenUsageProjector(IMediator mediator, ILogger<AevatarTokenUsageProjector> logger)
    {
        _mediator = mediator;
        _logger = logger;
        Task.Run(async () => { await ProcessCommand(); });
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

        if (_versionToTime.TryGetValue(grainId, out var versionData) && versionData.Item1 >= version)
        {
            return Task.CompletedTask;
        }

        var newVersionData = new Tuple<int, DateTime>(version, DateTime.Now);
        _versionToTime.AddOrUpdate(grainId, newVersionData, (id, existing) => newVersionData);

        _logger.LogDebug(
            $"[AevatarTokenUsageProjector] ProjectAsync get message:{JsonConvert.SerializeObject(aiGAgentState)}");
        var saveTokenCommand = new CQRS.Dto.TokenUsage
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
                var listGrain = new List<CQRS.Dto.TokenUsage>();
                while (_queue.TryDequeue(out CQRS.Dto.TokenUsage? message))
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
                await _mediator.Send(new TokenUsageCommand() { TokenUsages = listGrain });

                TryRemoveExpireGrain();
            }
        }

        // ReSharper disable once FunctionNeverReturns
    }

    private void TryRemoveExpireGrain()
    {
        int expireTime = 30;
        var dtNow = DateTime.Now;
        if ((dtNow - _lastUpdateTime).TotalSeconds <= expireTime)
        {
            return;
        }

        foreach (var item in _versionToTime)
        {
            if ((dtNow - item.Value.Item2).TotalSeconds >= expireTime)
            {
                _versionToTime.TryRemove(item);
            }
        }

        _lastUpdateTime = dtNow;
    }
}