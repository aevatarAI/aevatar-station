namespace Aevatar.Kubernetes.Abstractions;

public class LogOptions
{
    public int MaxLines { get; set; } = 1000;
    public bool Tail { get; set; } = true;
    public string? Since { get; set; }
    public string? Until { get; set; }
    public bool Follow { get; set; }
}