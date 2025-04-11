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

    public Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        if (IsValidStateWrapper(state))
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;
            int version = wrapper.Version;
            _logger.LogDebug("AevatarStateProjector GrainId {GrainId} Version {Version}", grainId.ToString(), version);
            var command = new SaveStateCommand
            {
                Id = grainId.ToString(),
                GuidKey = grainId.GetGuidKey().ToString(),
                State = wrapperState,
                Version = version
            };

            _latestCommands.AddOrUpdate(
                command.Id,
                command,
                (id, existing) => command.Version > existing.Version ? command : existing
            );
        }

        return Task.CompletedTask;
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
        const int maxRetries = 3;
        int retryCount = 0;
        List<SaveStateCommand> remainingCommands = new(batch);

        while (retryCount < maxRetries && remainingCommands.Count > 0)
        {
            try
            {
                var batchCommand = new SaveStateBatchCommand { Commands = remainingCommands };
                await _mediator.Send(batchCommand);
                _logger.LogInformation("Successfully sent {Count} commands", remainingCommands.Count);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Batch send failed (Attempt {RetryCount}/{MaxRetries})", retryCount, maxRetries);

                remainingCommands = GetValidCommands(remainingCommands);

                if (remainingCommands.Count == 0)
                {
                    _logger.LogInformation("All commands expired or succeeded");
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }

        _logger.LogError("Failed to send {Count} commands after {Retries} retries", remainingCommands.Count,
            maxRetries);
    }

    private List<SaveStateCommand> GetValidCommands(List<SaveStateCommand> commands)
    {
        var validCommands = new List<SaveStateCommand>();
        foreach (var cmd in commands)
        {
            if (_latestCommands.TryGetValue(cmd.Id, out var latest) && latest.Version == cmd.Version)
            {
                validCommands.Add(cmd);
            }
            else
            {
                _logger.LogDebug("Command {Id} v{Version} expired, latest is v{LatestVersion}",
                    cmd.Id, cmd.Version, latest?.Version ?? -1);
            }
        }

        return validCommands;
    }
}