using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Grains;
using Aevatar.Sandbox.Core.Streaming.Messages;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aevatar.Sandbox.Core.Streaming;

public sealed class SandboxExecDispatcher : IAsyncObserver<SandboxExecEnqueueMessage>
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<SandboxExecDispatcher> _logger;
    private readonly SemaphoreSlim _concurrency;

    public SandboxExecDispatcher(
        IClusterClient clusterClient,
        ILogger<SandboxExecDispatcher> logger,
        int maxConcurrency)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _concurrency = new SemaphoreSlim(maxConcurrency);
    }

    public async Task OnNextAsync(SandboxExecEnqueueMessage item, StreamSequenceToken? token = null)
    {
        await _concurrency.WaitAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                var grain = _clusterClient.GetGrain<ISandboxExecutionClientGrain>(item.ExecutionId);

                var result = await grain.ExecuteAsync(new SandboxExecutionClientParams
                {
                    Language = item.Language,
                    Code = item.Code,
                    Timeout = item.Timeout,
                    Resources = new ResourceLimits
                    {
                        CpuLimitCores = double.TryParse(item.CpuLimit, out var cpu) ? cpu : 1.0,
                        MemoryLimitBytes = long.TryParse(item.MemoryLimit, out var memory) ? memory : 512 * 1024 * 1024,
                        TimeoutSeconds = item.Timeout
                    }
                });

                await PublishResultAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sandbox execution request {ExecutionId}",
                    item.ExecutionId);
            }
            finally
            {
                _concurrency.Release();
            }
        });
    }

    public Task OnCompletedAsync() => Task.CompletedTask;

    public Task OnErrorAsync(Exception ex)
    {
        _logger.LogError(ex, "Error in sandbox execution dispatcher");
        return Task.CompletedTask;
    }

    private async Task PublishResultAsync(SandboxExecutionResult result)
    {
        try
        {
            var streamProvider = _clusterClient.GetStreamProvider(AevatarCoreConstants.StreamProvider);
            var streamId = StreamId.Create("sandbox.exec.results", result.ExecutionId);
            var stream = streamProvider.GetStream<SandboxExecResultMessage>(streamId);

            await stream.OnNextAsync(new SandboxExecResultMessage
            {
                SandboxExecutionId = result.ExecutionId,
                Success = result.Status == ExecutionStatus.Completed,
                Stdout = result.Output,
                Stderr = result.Error,
                TimedOut = result.Status == ExecutionStatus.TimedOut,
                ExecTimeSec = result.EndTime.HasValue && result.StartTime.HasValue
                    ? (result.EndTime.Value - result.StartTime.Value).TotalSeconds
                    : 0,
                MemoryUsedMB = result.ResourceUsage.MemoryUsageBytes / (1024 * 1024),
                ScriptHash = string.Empty,
                FinishedAtUtc = result.EndTime ?? DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish sandbox execution result {ExecutionId}",
                result.ExecutionId);
        }
    }
}