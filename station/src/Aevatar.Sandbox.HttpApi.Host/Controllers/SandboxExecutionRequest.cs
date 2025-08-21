namespace Aevatar.Sandbox.Controllers;

public class SandboxExecutionRequest
{
    public string Code { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
    public string Language { get; set; } = string.Empty;
    public SandboxResourceLimits? Resources { get; set; }
}

public class SandboxResourceLimits
{
    public double CpuLimitCores { get; set; } = 1.0;
    public long MemoryLimitBytes { get; set; } = 512 * 1024 * 1024;
}