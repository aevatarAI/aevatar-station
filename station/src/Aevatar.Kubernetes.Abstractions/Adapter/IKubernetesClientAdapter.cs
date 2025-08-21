using k8s.Models;

namespace Aevatar.Kubernetes.Abstractions.Adapter;

public interface IKubernetesClientAdapter
{
    Task<V1Job> CreateJobAsync(V1Job job, string namespaceParameter, CancellationToken ct = default);
    Task<V1Job> ReadNamespacedJobAsync(string name, string namespaceParameter, CancellationToken ct = default);
    Task DeleteJobAsync(string name, string namespaceParameter, CancellationToken ct = default);
    Task<V1PodList> ListNamespacedPodAsync(string namespaceParameter, CancellationToken ct = default);
    Task<string> ReadNamespacedPodLogAsync(string name, string namespaceParameter, string? container = null, bool? follow = null, int? tailLines = null, int? sinceSeconds = null, bool stderr = false, CancellationToken ct = default);
    Task<V1NetworkPolicy> CreateNetworkPolicyAsync(V1NetworkPolicy networkPolicy, string namespaceParameter, CancellationToken ct = default);
    Task DeleteNetworkPolicyAsync(string name, string namespaceParameter, CancellationToken ct = default);
    
    // Existing methods
    Task<V1ConfigMap> CreateConfigMapAsync(V1ConfigMap configMap, string namespaceParameter);
    Task<V1ConfigMap> ReadNamespacedConfigMapAsync(string name, string namespaceParameter);
    Task<V1ConfigMapList> ListConfigMapAsync(string namespaceParameter);
    Task<V1ConfigMap> ReplaceNamespacedConfigMapAsync(V1ConfigMap configMap, string name, string namespaceParameter);
    Task DeleteConfigMapAsync(string name, string namespaceParameter);
    Task<V1Deployment> CreateDeploymentAsync(V1Deployment deployment, string namespaceParameter);
    Task<V1Deployment> ReadNamespacedDeploymentAsync(string name, string namespaceParameter);
    Task<V1DeploymentList> ListDeploymentAsync(string namespaceParameter, string? labelSelector = null);
    Task<V1Deployment> ReplaceNamespacedDeploymentAsync(V1Deployment deployment, string name, string namespaceParameter);
    Task DeleteDeploymentAsync(string name, string namespaceParameter);
    Task<V1Service> CreateServiceAsync(V1Service service, string namespaceParameter);
    Task<V1ServiceList> ListServiceAsync(string namespaceParameter);
    Task DeleteServiceAsync(string name, string namespaceParameter);
    Task<V1Ingress> CreateIngressAsync(V1Ingress ingress, string namespaceParameter);
    Task<V1IngressList> ListIngressAsync(string namespaceParameter);
    Task DeleteIngressAsync(string name, string namespaceParameter);
    Task<V2HorizontalPodAutoscaler> ReadNamespacedHorizontalPodAutoscalerAsync(string name, string version);
    Task<V2HorizontalPodAutoscaler> CreateNamespacedHorizontalPodAutoscalerAsync(V2HorizontalPodAutoscaler horizontalPodAutoscaler, string namespaceParameter);
}