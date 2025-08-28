using System;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Core.GAgents.Events;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aevatar.Sandbox.Core.GAgents;

public sealed class SandboxExecutionGAgent : ISandboxExecutionGAgent
{
    private readonly ILogger<SandboxExecutionGAgent> _logger;

    public SandboxExecutionGAgent(ILogger<SandboxExecutionGAgent> logger)
    {
        _logger = logger;
    }

    public Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<SandboxExecutionResult?> TryGetResultAsync(string sandboxExecutionId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CancelAsync(string sandboxExecutionId)
    {
        throw new NotImplementedException();
    }
}