using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Grains;
using Aevatar.Sandbox.Core.Streaming.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Aevatar.Sandbox.Core.Services;

public class SandboxExecDispatcher
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<SandboxExecDispatcher> _logger;
    private readonly SandboxDispatcherOptions _options;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _languageThrottles;

    public SandboxExecDispatcher(
        IGrainFactory grainFactory,
        ILogger<SandboxExecDispatcher> logger,
        IOptions<SandboxDispatcherOptions> options)
    {
        _grainFactory = grainFactory;
        _logger = logger;
        _options = options.Value;
        _languageThrottles = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task<SandboxExecutionResult> EnqueueAsync(SandboxExecEnqueueMessage message, string? correlationId = null)
    {
        var throttle = _languageThrottles.GetOrAdd(message.Language, _ => new SemaphoreSlim(_options.MaxConcurrentExecutionsPerLanguage));

        try
        {
            if (!await throttle.WaitAsync(_options.QueueTimeout))
            {
                _logger.LogWarning("Queue timeout for language {Language}", message.Language);
                return new SandboxExecutionResult
                {
                    ExecutionId = message.ExecutionId,
                    Status = ExecutionStatus.Failed,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    Language = message.Language,
                    Error = "Queue timeout"
                };
            }

            try
            {
                var grain = _grainFactory.GetGrain<ISandboxExecutionClientGrain>(message.ExecutionId);
                return await grain.ExecuteAsync(new SandboxExecutionClientParams
                {
                    Code = message.Code,
                    Timeout = message.Timeout,
                    Language = message.Language,
                    Resources = new ResourceLimits
                    {
                        CpuLimitCores = double.TryParse(message.CpuLimit, out var cpu) ? cpu : 1.0,
                        MemoryLimitBytes = long.TryParse(message.MemoryLimit, out var memory) ? memory : 512 * 1024 * 1024,
                        TimeoutSeconds = message.Timeout
                    }
                });
            }
            finally
            {
                throttle.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue sandbox execution");
            return new SandboxExecutionResult
            {
                ExecutionId = message.ExecutionId,
                Status = ExecutionStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Language = message.Language,
                Error = ex.Message
            };
        }
    }
}