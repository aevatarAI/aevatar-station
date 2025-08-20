using k8s.Models;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Sandbox.Kubernetes.Manager;

public interface ISandboxKubernetesManager : ISingletonDependency
{
    Task<V1Job> CreateJobAsync(SandboxJobSpec spec, CancellationToken ct = default);
    Task<SandboxJobStatus> GetJobStatusAsync(string sandboxExecutionId, CancellationToken ct = default);
    Task<bool> DeleteJobAsync(string sandboxExecutionId, CancellationToken ct = default);
    Task<SandboxJobLogs> GetJobLogsAsync(string sandboxExecutionId, int? maxLines = null, bool includeStderr = true, string? since = null, CancellationToken ct = default);
}

public class SandboxJobSpec
{
    public required string SandboxExecutionId { get; init; }
    public required string Image { get; init; }
    public required string[] Command { get; init; }
    public Dictionary<string, string> Environment { get; init; } = new();
    public SandboxResourceLimits ResourceLimits { get; init; } = new();
    public NetworkPolicy NetworkPolicy { get; init; } = new();
}

public class SandboxResourceLimits
{
    public int CpuMillicores { get; init; } = 1000; // 1 vCPU
    public int MemoryMB { get; init; } = 512;
    public int TimeoutSeconds { get; init; } = 30;
}

public class NetworkPolicy
{
    public bool AllowEgress { get; init; }
    public string[]? AllowedHosts { get; init; }
}

public class SandboxJobStatus
{
    public bool IsComplete { get; init; }
    public int ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public double ExecutionTimeSeconds { get; init; }
    public int MemoryUsedMB { get; init; }
    public string ScriptHash { get; init; } = string.Empty;
    public DateTime FinishedAtUtc { get; init; }
}

public class SandboxJobLogs
{
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public bool Truncated { get; init; }
}