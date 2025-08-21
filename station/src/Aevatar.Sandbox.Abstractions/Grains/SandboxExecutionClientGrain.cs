using System;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Sandbox.Abstractions.Grains;

public class SandboxExecutionClientGrain : SandboxClientGrainBase
{
    public SandboxExecutionClientGrain(
        [PersistentState("execution")] IPersistentState<SandboxExecutionState> state,
        ISandboxService sandboxService,
        ILogger<SandboxExecutionClientGrain> logger)
        : base(state, sandboxService, logger)
    {
    }
}