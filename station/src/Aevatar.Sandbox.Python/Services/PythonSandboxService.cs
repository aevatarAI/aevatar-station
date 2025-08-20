using Aevatar.Sandbox.Abstractions.Services;
using Aevatar.Sandbox.Kubernetes.Manager;
using Microsoft.Extensions.Logging;

namespace Aevatar.Sandbox.Python.Services;

public sealed class PythonSandboxService : SandboxServiceBase
{
    protected override string LanguageId => "python";
    protected override string Image => "python-3.11-sandbox";
    protected override string[] CommandTemplate => new[] { "python", "/runner/entry.py" };

    protected override SandboxResourceLimits DefaultResourceLimits => new()
    {
        CpuMillicores = 1000, // 1 vCPU
        MemoryMB = 512,
        TimeoutSeconds = 30
    };

    protected override NetworkPolicy DefaultNetworkPolicy => new() { AllowEgress = false };

    public PythonSandboxService(ISandboxKubernetesManager kubernetes, ILogger<PythonSandboxService> logger)
        : base(kubernetes, logger)
    {
    }
}