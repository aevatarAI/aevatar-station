namespace Aevatar.Sandbox.Core.Services;

public class SandboxOptions
{
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public double DefaultCpuLimit { get; set; } = 1.0;
    public int DefaultMemoryLimit { get; set; } = 512;
    public string DefaultNamespace { get; set; } = "sandbox";
}