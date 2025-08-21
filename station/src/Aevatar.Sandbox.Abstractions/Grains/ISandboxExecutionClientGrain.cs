using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Orleans;

namespace Aevatar.Sandbox.Abstractions.Grains;

public interface ISandboxExecutionClientGrain : IGrainWithStringKey
{
    Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionClientParams parameters);
    Task<SandboxExecutionResult> GetResultAsync();
    Task<SandboxLogs> GetLogsAsync(LogQueryOptions? options = null);
    Task CancelAsync();
}