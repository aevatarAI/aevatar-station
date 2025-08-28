using k8s.Models;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Sandbox.Kubernetes.Adapter;

public interface ISandboxKubernetesClientAdapter : ISingletonDependency
{
    Task<V1Job> CreateJobAsync(V1Job job, string namespaceParameter, CancellationToken ct = default);
    Task<V1Job> ReadNamespacedJobAsync(string name, string namespaceParameter, CancellationToken ct = default);
    Task DeleteJobAsync(string name, string namespaceParameter, CancellationToken ct = default);
    Task<V1PodList> ListNamespacedPodAsync(string namespaceParameter, CancellationToken ct = default);
    Task<Stream> ReadNamespacedPodLogAsync(string name, string namespaceParameter, string? container = null, bool? follow = null, int? tailLines = null, int? sinceSeconds = null, CancellationToken ct = default);
    Task<V1NetworkPolicy> CreateNetworkPolicyAsync(V1NetworkPolicy networkPolicy, string namespaceParameter, CancellationToken ct = default);
    Task DeleteNetworkPolicyAsync(string name, string namespaceParameter, CancellationToken ct = default);
}