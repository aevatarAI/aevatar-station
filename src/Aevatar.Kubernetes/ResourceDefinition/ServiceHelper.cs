using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class ServiceHelper
{

    public static string GetAppServiceName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        return $"service-{appId}-{version}".ToLower();
    }
    
    public static string GetAppServicePortName(string version)
    {
        return $"service-{version}-port".ToLower();
    }

    public static string GetAppServiceLabelName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        return $"service-{appId}-{version}".ToLower();
    }

    public static V1Service CreateAppClusterIPServiceDefinition(string appId,string serviceName, string serviceLabelName,
        string deploymentLabelName, string servicePortName, int targetPort, int port = 80)
    {
        var service = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = serviceName,
                Labels = new Dictionary<string, string>
                {
                    { KubernetesConstants.AppLabelKey, serviceLabelName },
                    { KubernetesConstants.MonitorLabelKey, appId }
                },
                NamespaceProperty = KubernetesConstants.AppNameSpace
            },
            Spec = new V1ServiceSpec
            {
                Selector = new Dictionary<string, string>
                {
                    { KubernetesConstants.AppLabelKey, deploymentLabelName }
                },
                Ports = new List<V1ServicePort>
                {
                    new V1ServicePort
                    {
                        Name = servicePortName,
                        Protocol = "TCP",
                        Port = port,
                        TargetPort = targetPort,
                    }
                },
                Type = "ClusterIP"
            }
        };

        return service;
    }
}