namespace Aevatar.Sandbox.Kubernetes.Models;

public class NetworkPolicy
{
    public bool AllowEgress { get; init; }
    public string[]? AllowedEgressDomains { get; init; }

    public static NetworkPolicy NoEgress() => new() { AllowEgress = false };
    public static NetworkPolicy AllowAll() => new() { AllowEgress = true };
    public static NetworkPolicy AllowDomains(params string[] domains) => new() { AllowEgress = true, AllowedEgressDomains = domains };
}