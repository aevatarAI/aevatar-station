using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS;

public class AevatarStateProjector : IStateProjector, ISingletonDependency
{
    private readonly IMediator _mediator;
    private readonly ILogger<AevatarStateProjector> _logger;
    private readonly IDistributedCache _distributedcache;
    private readonly Dictionary<string, SaveStateCommand> _commandCache = new();
    private readonly object _lock = new();
    private readonly int _batchSize = 5;
    private int _writeSize = 0;
    private readonly TimeSpan _batchTimeout = TimeSpan.FromSeconds(1);
    private DateTime _lastBatchFlushTime = DateTime.UtcNow;

    public AevatarStateProjector(IMediator mediator, ILogger<AevatarStateProjector> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        if (state.GetType().IsGenericType &&
            state.GetType().GetGenericTypeDefinition() == typeof(StateWrapper<>) &&
            typeof(StateBase).IsAssignableFrom(state.GetType().GetGenericArguments()[0]))
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;
            _logger.LogDebug("AevatarStateProjector GrainId {GrainId}", grainId.ToString());
            var command = new SaveStateCommand
            {
                Id = grainId.GetGuidKey().ToString(),
                State = wrapperState
            };

            AddToCache(command);

            _logger.LogDebug("AevatarStateProjector GrainId {GrainId} cached", grainId.ToString());
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid state type: {state.GetType().Name}. Expected StateWrapper<T> where T : StateBase.");
        }
    }

    private void AddToCache(SaveStateCommand command)
    {
        lock (_lock)
        {
            _commandCache[command.Id] = command;
            _writeSize++;
            if (_writeSize >= _batchSize || DateTime.UtcNow - _lastBatchFlushTime >= _batchTimeout)
            {
                _ = FlushCacheAsync();
            }
        }
    }

    private async Task FlushCacheAsync()
    {
        List<SaveStateCommand> batch;

        lock (_lock)
        {
            if (_commandCache.Count == 0)
            {
                return;
            }

            batch = new List<SaveStateCommand>(_commandCache.Values);
            _commandCache.Clear();
            _writeSize = 0;
            _lastBatchFlushTime = DateTime.UtcNow;
        }

        try
        {
            foreach (var saveState in batch)
            {
                await _mediator.Send(saveState);
            }

            _logger.LogDebug("Successfully flushed {Count} items to storage.", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush batched commands to storage.");
            lock (_lock)
            {
                foreach (var cmd in batch)
                {
                    _commandCache[cmd.Id] = cmd;
                }
            }
        }
    }
}