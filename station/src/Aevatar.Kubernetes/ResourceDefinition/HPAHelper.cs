using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class HPAHelper
{
    public static V2HorizontalPodAutoscaler CreateHPA(string appId,string version)
    {
        return new V2HorizontalPodAutoscaler
        {
            ApiVersion = "autoscaling/v2",
            Kind = "HorizontalPodAutoscaler",
            Metadata = new V1ObjectMeta
            {
                Name = $"{appId}-hpa",
                NamespaceProperty = KubernetesConstants.AppNameSpace
            },
            Spec = new V2HorizontalPodAutoscalerSpec
            {
                ScaleTargetRef = new V2CrossVersionObjectReference
                {
                    ApiVersion = "apps/v1",
                    Kind = "Deployment",
                    Name = DeploymentHelper.GetAppDeploymentName(appId, version) 
                },
                MinReplicas = 1,
                MaxReplicas = 3,
                Metrics = new List<V2MetricSpec>
                {
                    new V2MetricSpec
                    {
                        Type = "Resource",
                        Resource = new V2ResourceMetricSource
                        {
                            Name = "cpu",
                            Target = new V2MetricTarget
                            {
                                Type = "Utilization",
                                AverageUtilization = 50 
                            }
                        }
                    }
                }
            }
        };
    }
}