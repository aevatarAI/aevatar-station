using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class ServiceMonitorHelper
{
    public static string GetAppServiceMonitorName(string appId)
    {
        appId = appId.Replace("_", "-");
        return $"service-monitor-{appId}".ToLower();
    }

    public static ServiceMonitor CreateAppServiceMonitorDefinition(string appId, string serviceMonitorName,
        string servicePortName, string metricsPath)
    {
        var serviceMonitor = new ServiceMonitor
        {
            Metadata = new V1ObjectMeta
            {
                Name = serviceMonitorName,
                Labels = new Dictionary<string, string>
                {
                    { "release", "prometheus" },
                    { KubernetesConstants.MonitorLabelKey, appId }
                },
                NamespaceProperty = KubernetesConstants.AppNameSpace
            },
            Spec = new ServiceMonitorSpec
            {
                Endpoints = new List<Endpoint>
                {
                    new Endpoint
                    {
                        Port = servicePortName,
                        Interval = "15s",
                        Path = metricsPath,
                        Relabelings = new List<Relabeling>()
                        {
                            new Relabeling()
                            {
                                Action = "replace",
                                Replacement = appId,
                                SourceLabels = new List<string>(),
                                TargetLabel = KubernetesConstants.MonitorLabelKey
                            }
                        }
                    }
                },
                NamespaceSelector = new NamespaceSelector
                {
                    MatchNames = new List<string> { KubernetesConstants.AppNameSpace }
                },
                JobLabel = KubernetesConstants.MonitorLabelKey,
                Selector = new Selector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { KubernetesConstants.MonitorLabelKey, appId }
                    }
                }
            }
        };

        return serviceMonitor;
    }
}