using System.ComponentModel.DataAnnotations;

namespace Aevatar.Sandbox.Controllers;

/// <summary>
/// Represents a request to execute code in a sandbox environment
/// </summary>
public class SandboxExecutionRequest
{
    /// <summary>
    /// The code to execute
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// The execution timeout in seconds (default: 30)
    /// </summary>
    public int Timeout { get; set; } = 30;
    
    /// <summary>
    /// The programming language to use (e.g., "python", "javascript")
    /// </summary>
    public string Language { get; set; } = string.Empty;
    
    /// <summary>
    /// The resource limits for the execution
    /// </summary>
    public SandboxResourceLimits? Resources { get; set; }
}

/// <summary>
/// Represents resource limits for sandbox execution
/// </summary>
public class SandboxResourceLimits
{
    /// <summary>
    /// CPU limit in cores (default: 1.0)
    /// </summary>
    public double CpuLimitCores { get; set; } = 1.0;
    
    /// <summary>
    /// Memory limit in bytes (default: 512MB)
    /// </summary>
    public long MemoryLimitBytes { get; set; } = 512 * 1024 * 1024;
}