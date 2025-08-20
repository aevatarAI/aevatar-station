using Aevatar.Sandbox.Kubernetes.Manager;
using Aevatar.Sandbox.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Aevatar.Sandbox.Abstractions.Services;

public abstract class SandboxServiceBase : ISandboxService
{
    protected readonly ISandboxKubernetesManager Kubernetes;
    protected readonly ILogger Logger;

    protected SandboxServiceBase(ISandboxKubernetesManager kubernetes, ILogger logger)
    {
        Kubernetes = kubernetes;
        Logger = logger;
    }

    protected abstract string LanguageId { get; }
    protected abstract string Image { get; }
    protected abstract string[] CommandTemplate { get; }

    protected virtual SandboxResourceLimits DefaultResourceLimits => new()
    {
        CpuMillicores = 1000, // 1 vCPU
        MemoryMB = 512,
        TimeoutSeconds = 30
    };

    protected virtual NetworkPolicy DefaultNetworkPolicy => new()
    {
        AllowEgress = false
    };

    public virtual async Task<SandboxExecutionHandle> StartAsync(SandboxExecutionRequest request, CancellationToken ct = default)
    {
        if (request.LanguageId != LanguageId)
            throw new InvalidOperationException($"Language mismatch. Expected: {LanguageId}, Got: {request.LanguageId}");

        var spec = BuildJobSpec(request);
        var job = await Kubernetes.CreateJobAsync(spec, ct);

        return new SandboxExecutionHandle
        {
            SandboxExecutionId = request.SandboxExecutionId,
            WorkloadName = job.Metadata.Name,
            StartedAtUtc = DateTime.UtcNow
        };
    }

    public virtual async Task<SandboxExecutionResult?> TryGetResultAsync(string sandboxExecutionId, CancellationToken ct = default)
    {
        var status = await Kubernetes.GetJobStatusAsync(sandboxExecutionId, ct);
        if (!status.IsComplete)
            return null;

        var logs = await GetLogsAsync(sandboxExecutionId, new LogQueryOptions(), ct);
        return new SandboxExecutionResult
        {
            SandboxExecutionId = sandboxExecutionId,
            Success = status.ExitCode == 0,
            Stdout = logs.Stdout,
            Stderr = logs.Stderr,
            ExitCode = status.ExitCode,
            TimedOut = status.TimedOut,
            ExecTimeSec = status.ExecutionTimeSeconds,
            MemoryUsedMB = status.MemoryUsedMB,
            ScriptHash = status.ScriptHash,
            FinishedAtUtc = status.FinishedAtUtc
        };
    }

    public virtual Task<bool> CancelAsync(string sandboxExecutionId, CancellationToken ct = default)
    {
        return Kubernetes.DeleteJobAsync(sandboxExecutionId, ct);
    }

    public virtual async Task<SandboxLogs> GetLogsAsync(string sandboxExecutionId, LogQueryOptions options, CancellationToken ct = default)
    {
        var logs = await Kubernetes.GetJobLogsAsync(sandboxExecutionId, options.MaxLines, options.IncludeStderr, options.Since, ct);
        return new SandboxLogs
        {
            SandboxExecutionId = sandboxExecutionId,
            Stdout = logs.Stdout,
            Stderr = logs.Stderr,
            Truncated = logs.Truncated
        };
    }

    protected virtual SandboxJobSpec BuildJobSpec(SandboxExecutionRequest request)
    {
        return new SandboxJobSpec
        {
            SandboxExecutionId = request.SandboxExecutionId,
            Image = Image,
            Command = CommandTemplate,
            Environment = new Dictionary<string, string>
            {
                ["SANDBOX_EXECUTION_ID"] = request.SandboxExecutionId,
                ["CODE"] = request.Code,
                ["TIMEOUT_SECONDS"] = request.TimeoutSeconds.ToString(),
                ["TENANT_ID"] = request.TenantId ?? string.Empty,
                ["CHAT_ID"] = request.ChatId ?? string.Empty
            },
            ResourceLimits = DefaultResourceLimits,
            NetworkPolicy = DefaultNetworkPolicy
        };
    }
}