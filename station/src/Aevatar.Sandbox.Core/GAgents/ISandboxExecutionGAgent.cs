using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;

namespace Aevatar.Sandbox.Core.GAgents;

public interface ISandboxExecutionGAgent
{
    Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionRequest request);
    Task<SandboxExecutionResult?> TryGetResultAsync(string sandboxExecutionId);
    Task<bool> CancelAsync(string sandboxExecutionId);
}