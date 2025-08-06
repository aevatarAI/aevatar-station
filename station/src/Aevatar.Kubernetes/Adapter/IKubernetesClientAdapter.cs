using Aevatar.Kubernetes.ResourceDefinition;
using k8s.Models;

namespace Aevatar.Kubernetes.Adapter;

public interface IKubernetesClientAdapter
{
    Task<V1NamespaceList> ListNamespaceAsync();

    Task<V1Namespace> ReadNamespaceAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<(V1PodList, string)> ListPodsInNamespaceWithPagingAsync(string namespaceParameter, int pageSize,
        string continueToken, CancellationToken cancellationToken = default(CancellationToken));

    Task<V1PodList> ListPodsInNamespaceWithPagingAsync(string namespaceParameter, string labelSelector,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1ConfigMapList> ListConfigMapAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1DeploymentList> ListDeploymentAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1DeploymentList> ListDeploymentAsync(string namespaceParameter, string labelSelector,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1ServiceList> ListServiceAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1IngressList> ListIngressAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<object> ListServiceMonitorAsync(string monitorGroup, string coreApiVersion,
        string namespaceParameter, string monitorPlural);

    Task<V1Namespace> CreateNamespaceAsync(V1Namespace nameSpace,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1ConfigMap> CreateConfigMapAsync(V1ConfigMap configMap, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Deployment> CreateDeploymentAsync(V1Deployment deployment, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Service> CreateServiceAsync(V1Service service, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Ingress> CreateIngressAsync(V1Ingress ingress, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<object> CreateServiceMonitorAsync(ServiceMonitor serviceMonitor, string monitorGroup,
        string coreApiVersion, string namespaceParameter,
        string monitorPlural, CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Status> DeleteConfigMapAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));
    
    Task<V1Status> DeleteDeploymentAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));
    
    Task<V1Service> DeleteServiceAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));
    
    Task<V1Status> DeleteIngressAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<object> DeleteServiceMonitorAsync(string monitorGroup, string coreApiVersion,
        string namespaceParameter, string monitorPlural, string serviceMonitorName);
    
    Task<V1Deployment> ReadNamespacedDeploymentAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Deployment> ReplaceNamespacedDeploymentAsync(V1Deployment deployment, string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1ConfigMap> ReadNamespacedConfigMapAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1ConfigMap> ReplaceNamespacedConfigMapAsync(V1ConfigMap configMap, string name,
        string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<PodMetricsList> GetKubernetesPodsMetricsByNamespaceAsync(string namespaceParameter);

    public Task<V2HorizontalPodAutoscaler> ReadNamespacedHorizontalPodAutoscalerAsync(string name,
        string namespaceParameter, CancellationToken cancellationToken = default(CancellationToken));

    public Task<V2HorizontalPodAutoscaler> CreateNamespacedHorizontalPodAutoscalerAsync(V2HorizontalPodAutoscaler body,
        string namespaceParameter, CancellationToken cancellationToken = default(CancellationToken));

    Task<bool> NamespaceExistsAsync(string namespaceName, CancellationToken cancellationToken = default(CancellationToken));

}