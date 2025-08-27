using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Aevatar.Sandbox.Kubernetes.Adapter;

public class SandboxKubernetesClientAdapter : ISandboxKubernetesClientAdapter
{
    private readonly IKubernetes _client;
    private readonly ILogger<SandboxKubernetesClientAdapter> _logger;

    public SandboxKubernetesClientAdapter(IKubernetes client, ILogger<SandboxKubernetesClientAdapter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<V1Job> CreateJobAsync(V1Job job, string namespaceParameter, CancellationToken ct = default)
    {
        return await _client.BatchV1.CreateNamespacedJobAsync(job, namespaceParameter, cancellationToken: ct);
    }

    public async Task<V1Job> ReadNamespacedJobAsync(string name, string namespaceParameter, CancellationToken ct = default)
    {
        return await _client.BatchV1.ReadNamespacedJobAsync(name, namespaceParameter, cancellationToken: ct);
    }

    public async Task DeleteJobAsync(string name, string namespaceParameter, CancellationToken ct = default)
    {
        await _client.BatchV1.DeleteNamespacedJobAsync(name, namespaceParameter, cancellationToken: ct);
    }

    public async Task<V1PodList> ListNamespacedPodAsync(string namespaceParameter, CancellationToken ct = default)
    {
        return await _client.CoreV1.ListNamespacedPodAsync(namespaceParameter, cancellationToken: ct);
    }

    public async Task<Stream> ReadNamespacedPodLogAsync(string name, string namespaceParameter, string? container = null, bool? follow = null, int? tailLines = null, int? sinceSeconds = null, CancellationToken ct = default)
    {
        return await _client.CoreV1.ReadNamespacedPodLogAsync(name, namespaceParameter, container: container, follow: follow, tailLines: tailLines, sinceSeconds: sinceSeconds, cancellationToken: ct);
    }

    public async Task<V1NetworkPolicy> CreateNetworkPolicyAsync(V1NetworkPolicy networkPolicy, string namespaceParameter, CancellationToken ct = default)
    {
        return await _client.NetworkingV1.CreateNamespacedNetworkPolicyAsync(networkPolicy, namespaceParameter, cancellationToken: ct);
    }

    public async Task DeleteNetworkPolicyAsync(string name, string namespaceParameter, CancellationToken ct = default)
    {
        await _client.NetworkingV1.DeleteNamespacedNetworkPolicyAsync(name, namespaceParameter, cancellationToken: ct);
    }
}