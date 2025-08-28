using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;

namespace Aevatar.Sandbox.Abstractions.Services;

public interface ISandboxService
{
    Task<SandboxExecutionResult> ExecuteAsync(string code, int timeout, ResourceLimits resources);
    Task<SandboxLogs> GetLogsAsync(string executionId, LogQueryOptions? options = null);
    Task CancelAsync(string executionId);
    Task<SandboxExecutionResult> GetStatusAsync(string executionId);
}