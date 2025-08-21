using System;

namespace Aevatar.Kubernetes.Abstractions;

public class KubernetesJobResult
{
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string CpuUsage { get; set; } = string.Empty;
    public string MemoryUsage { get; set; } = string.Empty;
    public string NetworkIn { get; set; } = string.Empty;
    public string NetworkOut { get; set; } = string.Empty;
    public string DiskRead { get; set; } = string.Empty;
    public string DiskWrite { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}