using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

public class AevatarStateProjector : IStateProjector, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, SaveStateCommand> _latestCommands = new();
    private readonly IMediator _mediator;
    private readonly ILogger<AevatarStateProjector> _logger;
    private readonly ProjectorBatchOptions _batchOptions;

    public AevatarStateProjector(
        IMediator mediator,
        ILogger<AevatarStateProjector> logger,
        IOptionsSnapshot<ProjectorBatchOptions> options)
    {
        _mediator = mediator;
        _logger = logger;
        _batchOptions = options.Value;
        _ = ProcessCommandsAsync();
    }

    public async Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        if (IsValidStateWrapper(state))
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;
            _logger.LogDebug("AevatarStateProjector GrainId {GrainId}",grainId.ToString());
            var command = new SaveStateCommand
            {
                Id = grainId.ToString(),
                GuidKey = grainId.GetGuidKey().
                    ToString("N"),
                State = wrapperState,
                Version = wrapper.Version
            };

            _latestCommands.AddOrUpdate(
                command.Id,
                command,
                (id, existing) => command.Version > existing.Version ? command : existing
            );
        }
    }

    private async Task ProcessCommandsAsync()
    {
        while (true)
        {
            try
            {
                if (_latestCommands.Count < _batchOptions.BatchSize)
                {
                    await Task.Delay(_batchOptions.BatchTimeoutSeconds * 1000);
                }

                var currentBatch = _latestCommands.Values
                    .OrderByDescending(c => c.Version)
                    .Take(_batchOptions.BatchSize)
                    .ToList();

                if (currentBatch.Count > 0)
                {
                    _logger.LogInformation("latestCommands count :{Count} ", _latestCommands.Count);
                    await SendBatchAsync(currentBatch);
                    CleanProcessedCommands(currentBatch);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch processing failed");
            }
        }
    }

    private void CleanProcessedCommands(List<SaveStateCommand> processed)
    {
        foreach (var cmd in processed)
        {
            _latestCommands.TryGetValue(cmd.Id, out var current);
            if (current != null && current.Version <= cmd.Version)
            {
                _latestCommands.TryRemove(cmd.Id, out _);
            }
        }
    }


    private bool IsValidStateWrapper<T>(T state) where T : StateWrapperBase
    {
        return state.GetType().IsGenericType &&
               state.GetType().GetGenericTypeDefinition() == typeof(StateWrapper<>) &&
               typeof(StateBase).IsAssignableFrom(state.GetType().GetGenericArguments()[0]);
    }

    private async Task SendBatchAsync(List<SaveStateCommand> batch)
    {
        try
        {
            var batchCommand = new SaveStateBatchCommand();
            batchCommand.Commands = batch;
            await _mediator.Send(batchCommand);
            _logger.LogInformation("Sent {Count} commands", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch send failed");

            await RetryBatchAsync(batch, maxRetries: 3);
        }
    }

    private async Task RetryBatchAsync(List<SaveStateCommand> batch, int maxRetries)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            try
            {
                var validCommands = batch
                    .Where(c => _latestCommands.TryGetValue(c.Id, out var latest) && latest.Version == c.Version)
                    .ToList();

                if (validCommands.Count == 0) return;
                await SendBatchAsync(validCommands);
                return;
            }
            catch
            {
                _logger.LogWarning("Retry {RetryCount}/3 failed", i + 1);
            }
        }

        _logger.LogError("Batch failed after {Retries} retries", maxRetries);
    }
}