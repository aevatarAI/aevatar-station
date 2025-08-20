using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Grains;
using Aevatar.Sandbox.Core.Streaming.Messages;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Aevatar.Sandbox.Core.Streaming;

public sealed class SandboxExecDispatcher : IAsyncObserver<SandboxExecEnqueueMessage>
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<SandboxExecDispatcher> _logger;
    private readonly SemaphoreSlim _concurrency;

    public SandboxExecDispatcher(
        IGrainFactory grainFactory,
        ILogger<SandboxExecDispatcher> logger,
        int maxConcurrency)
    {
        _grainFactory = grainFactory;
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
                var grain = _grainFactory.GetGrain<ISandboxExecutionClientGrain>(
                    Guid.Parse(item.SandboxExecutionId));

                var result = await grain.ExecuteAsync(new SandboxExecutionClientParams
                {
                    LanguageId = item.LanguageId,
                    Code = item.Code,
                    TimeoutSeconds = item.TimeoutSeconds,
                    TenantId = item.TenantId,
                    ChatId = item.ChatId
                });

                await PublishResultAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sandbox execution request {SandboxExecutionId}",
                    item.SandboxExecutionId);
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
            var streamProvider = GetStreamProvider("kafka");
            var stream = streamProvider.GetStream<SandboxExecResultMessage>(
                StreamId.Create("sandbox.exec.results", result.SandboxExecutionId));

            await stream.OnNextAsync(new SandboxExecResultMessage
            {
                SandboxExecutionId = result.SandboxExecutionId,
                Success = result.Success,
                Stdout = result.Stdout,
                Stderr = result.Stderr,
                ExitCode = result.ExitCode,
                TimedOut = result.TimedOut,
                ExecTimeSec = result.ExecTimeSec,
                MemoryUsedMB = result.MemoryUsedMB,
                ScriptHash = result.ScriptHash,
                FinishedAtUtc = result.FinishedAtUtc
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish sandbox execution result {SandboxExecutionId}",
                result.SandboxExecutionId);
        }
    }
}