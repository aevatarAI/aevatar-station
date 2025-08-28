using Aevatar.Kubernetes.Abstractions;
using Aevatar.Sandbox.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Sandbox.Python.Services;

public class PythonSandboxService : SandboxServiceBase
{
    private readonly string _pythonImage;
    private readonly string _namespace;

    public PythonSandboxService(
        ILogger<PythonSandboxService> logger,
        IKubernetesHostManager kubernetesManager,
        IOptions<SandboxOptions> options,
        IOptions<PythonSandboxOptions> pythonOptions,
        SandboxExecDispatcher dispatcher)
        : base(logger, kubernetesManager, options, dispatcher)
    {
        _pythonImage = pythonOptions.Value.PythonImage;
        _namespace = pythonOptions.Value.Namespace;
    }

    protected override string GetImage() => _pythonImage;
    protected override string GetNamespace() => _namespace;
    protected override string GetLanguage() => "python";

    protected override string[] GetCommand(string code)
    {
        return new[]
        {
            "python",
            "-c",
            code
        };
    }
}