using System.Threading;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;

namespace Aevatar.Sandbox.Abstractions.Services;

public interface ISandboxService
{
    Task<SandboxExecutionHandle> StartAsync(SandboxExecutionRequest request, CancellationToken ct = default);
    Task<SandboxExecutionResult?> TryGetResultAsync(string sandboxExecutionId, CancellationToken ct = default);
    Task<bool> CancelAsync(string sandboxExecutionId, CancellationToken ct = default);
    Task<SandboxLogs> GetLogsAsync(string sandboxExecutionId, LogQueryOptions options, CancellationToken ct = default);
}

public sealed class LogQueryOptions
{
    public int? MaxLines { get; init; }
    public bool IncludeStderr { get; init; } = true;
    public string? Since { get; init; }
}

public sealed class SandboxLogs
{
    public required string SandboxExecutionId { get; init; }
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public bool Truncated { get; init; }
}