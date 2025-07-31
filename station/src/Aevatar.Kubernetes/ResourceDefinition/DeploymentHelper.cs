using Aevatar.Domain.Shared.Configuration;
using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class DeploymentHelper
{
    public static string GetAppDeploymentName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        var name = $"deployment-{appId}-{version}";
        return name.ToLower();
    }

    /// <summary>
    /// label name must be no more than 63 characters
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public static string GetAppDeploymentLabelName(string appId, string version)
    {
        var name = $"deployment-{appId}-{version}";

        return name.ToLower();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="version"></param>
    /// <param name="imageName"></param>
    /// <param name="deploymentName"></param>
    /// <param name="deploymentLabelName">must be no more than 63 characters</param>
    /// <param name="command"></param>
    /// <param name="replicasCount"></param>
    /// <param name="containerName"></param>
    /// <param name="containerPort"></param>
    /// <param name="configMapName"></param>
    /// <param name="sideCarConfigMapName"></param>
    /// <param name="requestCpu"></param>
    /// <param name="requestMemory"></param>
    /// <param name="maxSurge"></param>
    /// <param name="maxUnavailable"></param>
    /// <param name="isSilo"></param>
    /// <param name="readinessProbeHealthPath"></param>
    /// <returns></returns>
    public static V1Deployment CreateAppDeploymentWithFileBeatSideCarDefinition(string appId, string version,
        string imageName, string deploymentName, string deploymentLabelName, List<string> command,
        int replicasCount, string containerName, int containerPort, string configMapName, string sideCarConfigMapName,
        string requestCpu, string requestMemory, string maxSurge, string maxUnavailable,
        bool isSilo, string readinessProbeHealthPath = null)
    {
        var labels = CreateLabels(deploymentLabelName, appId, version);
        V1EnvVar[] MainEnv = null;
        if (isSilo)
        {
            MainEnv = GetSiloV1Env(RemoveSuffix(appId.ToLower(), "-" + KubernetesConstants.HostSilo));
        }

        V1EnvVar[] otherEnv = GetV1Env();
        var deployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = deploymentName,
                NamespaceProperty = KubernetesConstants.AppNameSpace,
                Labels = labels
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = replicasCount,
                Selector = new V1LabelSelector { MatchLabels = labels },
                Strategy = CreateStrategy(maxSurge, maxUnavailable),
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = labels },
                    Spec = new V1PodSpec
                    {
                        Affinity = CreateNodeAffinity(),
                        Tolerations = CreateNodeTolerations(),
                        Containers = CreateContainers(imageName, containerName, command, containerPort,
                            requestCpu, requestMemory, readinessProbeHealthPath, isSilo ? MainEnv : otherEnv, otherEnv, isSilo),
                        Volumes = CreatePodTemplateVolumes(configMapName, sideCarConfigMapName)
                    }
                }
            }
        };

        return deployment;
    }

    private static Dictionary<string, string> CreateLabels(string deploymentLabelName, string appId, string version)
    {
        return new Dictionary<string, string>
        {
            { KubernetesConstants.AppLabelKey, deploymentLabelName },
            { KubernetesConstants.MonitorLabelKey, appId },
            { KubernetesConstants.AppIdLabelKey, appId },
            { KubernetesConstants.AppVersionLabelKey, version }
            //  { KubernetesConstants.AppPodTypeLabelKey, podType }
        };
    }

    public static string RemoveSuffix(string str, string suffix)
    {
        if (str.EndsWith(suffix))
        {
            return str.Substring(0, str.Length - suffix.Length);
        }

        return str;
    }

    private static V1DeploymentStrategy CreateStrategy(string maxSurge, string maxUnavailable)
    {
        return new V1DeploymentStrategy
        {
            Type = "RollingUpdate",
            RollingUpdate = new V1RollingUpdateDeployment
            {
                MaxSurge = maxSurge,
                MaxUnavailable = maxUnavailable
            }
        };
    }

    private static IList<V1Toleration> CreateNodeTolerations()
    {
        return
        [
            new V1Toleration
            {
                Effect = "NoSchedule",
                Key = "kubernetes.io/dedicated",
                OperatorProperty = "Equal",
                Value = "ai"
            }
        ];
    }

    private static V1Affinity CreateNodeAffinity()
    {
        return new V1Affinity
        {
            NodeAffinity = new V1NodeAffinity
            {
                RequiredDuringSchedulingIgnoredDuringExecution = new V1NodeSelector
                {
                    NodeSelectorTerms = new List<V1NodeSelectorTerm>
                    {
                        new V1NodeSelectorTerm
                        {
                            MatchExpressions = new List<V1NodeSelectorRequirement>
                            {
                                new V1NodeSelectorRequirement
                                {
                                    Key = "resource",
                                    OperatorProperty = "In",
                                    Values = new List<string> { KubernetesConstants.NodeAffinityValue }
                                },
                                new V1NodeSelectorRequirement
                                {
                                    Key = "app",
                                    OperatorProperty = "In",
                                    Values = new List<string> { KubernetesConstants.NodeAffinityValue }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private static List<V1Container> CreateContainers(string imageName, string containerName, List<string> command,
        int containerPort,
        string requestCpu, string requestMemory, string readinessProbeHealthPath, V1EnvVar[] env, V1EnvVar[] sidecarEnv,
        bool isSilo = false)
    {
        // Main container
        var ports = new List<V1ContainerPort> { new V1ContainerPort(containerPort) };
        
        // For Silo containers, add health check port
        if (isSilo)
        {
            ports.Add(new V1ContainerPort(KubernetesConstants.SiloHealthCheckPort));
        }
        
        var mainContainer = new V1Container
        {
            Name = containerName,
            Image = imageName,
            Command = command,
            Ports = ports,
            VolumeMounts = CreateMainContainerVolumeMounts(),
            Resources = CreateResources(requestCpu, requestMemory),
            Env = env
        };
        if (!string.IsNullOrEmpty(readinessProbeHealthPath))
        {
            // Use health check port for Silo readiness probe, otherwise use main container port
            var probePort = isSilo ? KubernetesConstants.SiloHealthCheckPort : containerPort;
            mainContainer.ReadinessProbe = CreateQueryPodReadinessProbe(readinessProbeHealthPath, probePort);;
            mainContainer.LivenessProbe = CreateQueryPodLivenessProbe(readinessProbeHealthPath, probePort);
        }

        // Filebeat side car container
        var sideCarContainer = new V1Container
        {
            Name = KubernetesConstants.FileBeatContainerName,
            Image = KubernetesConstants.FileBeatImage,
            Args = new List<string>
            {
                "-c", KubernetesConstants.FileBeatConfigMountPath,
                "-e",
            },
            Env = sidecarEnv,
            VolumeMounts = CreateSideCarContainerVolumeMounts()
        };

        return new List<V1Container>
        {
            mainContainer, sideCarContainer
        };
    }

    private static V1EnvVar[] GetV1Env()
    {
        return new V1EnvVar[]
        {
            new V1EnvVar
            {
                Name = "POD_IP",
                ValueFrom = new V1EnvVarSource
                {
                    FieldRef = new V1ObjectFieldSelector
                    {
                        FieldPath = "status.podIP"
                    }
                }
            }
        };
    }

    private static V1EnvVar[] GetSiloV1Env(string appId)
    {
        return new V1EnvVar[]
        {
            new V1EnvVar("ORLEANS_SERVICE_ID", $"{appId}BasicService"),
            new V1EnvVar("ORLEANS_CLUSTER_ID", $"{appId}SiloCluster"),
            new V1EnvVar
            {
                Name = "POD_NAMESPACE",
                ValueFrom = new V1EnvVarSource
                {
                    FieldRef = new V1ObjectFieldSelector
                    {
                        FieldPath = "metadata.namespace"
                    }
                }
            },
            new V1EnvVar
            {
                Name = "POD_NAME",
                ValueFrom = new V1EnvVarSource
                {
                    FieldRef = new V1ObjectFieldSelector
                    {
                        FieldPath = "metadata.name"
                    }
                }
            },
            new V1EnvVar
            {
                Name = "POD_IP",
                ValueFrom = new V1EnvVarSource
                {
                    FieldRef = new V1ObjectFieldSelector
                    {
                        FieldPath = "status.podIP"
                    }
                }
            },
            new V1EnvVar("SILO_NAME_PATTERN", "Projector")
        };
    }

    public static V1ResourceRequirements CreateResources(string requestCpu, string requestMemory)
    {
        return new V1ResourceRequirements
        {
            Requests = new Dictionary<string, ResourceQuantity>()
            {
                { "cpu", new ResourceQuantity(requestCpu) },
                { "memory", new ResourceQuantity(requestMemory) }
            }
        };
    }

    private static V1Probe CreateQueryPodReadinessProbe(string readinessProbeHealthPath, int containerPort)
    {
        return new V1Probe()
        {
            HttpGet = new V1HTTPGetAction()
            {
                Path = readinessProbeHealthPath,
                Port = containerPort
            },
            InitialDelaySeconds = 5,
            PeriodSeconds = 5,
            TimeoutSeconds = 1,
            SuccessThreshold = 2,
            FailureThreshold = 10
        };
    }
    
    private static V1Probe CreateQueryPodLivenessProbe(string livenessProbeHealthPath, int containerPort)
    {
        return new V1Probe()
        {
            HttpGet = new V1HTTPGetAction()
            {
                Path = livenessProbeHealthPath, 
                Port = containerPort
            },
            InitialDelaySeconds = 10,     
            PeriodSeconds = 10,          
            TimeoutSeconds = 2,          
            SuccessThreshold = 1,         
            FailureThreshold = 3 
        };
    }

    private static List<V1VolumeMount> CreateMainContainerVolumeMounts()
    {
        return new List<V1VolumeMount>
        {
            new V1VolumeMount
            {
                Name = "config-volume",
                MountPath = KubernetesConstants
                    .AppSettingFileMountPath, // Change to the directory where you want to mount
                SubPath = KubernetesConstants.AppSettingFileName
            },
            new V1VolumeMount
            {
                Name = "config-volume",
                MountPath = KubernetesConstants
                    .AppSettingSharedFileMountPath,
                SubPath = KubernetesConstants.AppSettingSharedFileName
            },
            new V1VolumeMount
            {
                Name = "config-volume",
                MountPath = KubernetesConstants
                    .AppSettingHttpApiHostSharedFileMountPath,
                SubPath = KubernetesConstants.AppSettingHttpApiHostSharedFileName
            },
            new V1VolumeMount
            {
                Name = "config-volume",
                MountPath = KubernetesConstants
                    .AppSettingSiloSharedFileMountPath,
                SubPath = KubernetesConstants.AppSettingSiloSharedFileName
            },
            new V1VolumeMount
            {
                Name = "config-volume",
                MountPath = KubernetesConstants
                    .AppSettingBusinessFileMountPath,
                SubPath = SecureConfigurationExtensions.DefaultBusinessConfigPath
            },
            new V1VolumeMount
            {
                Name = "log-volume",
                MountPath = KubernetesConstants.AppLogFileMountPath
            }
        };
    }

    private static List<V1VolumeMount> CreateSideCarContainerVolumeMounts()
    {
        return new List<V1VolumeMount>
        {
            new V1VolumeMount
            {
                Name = "log-volume",
                MountPath = KubernetesConstants.AppLogFileMountPath
            },
            new V1VolumeMount
            {
                Name = "sidecar-config-volume",
                MountPath = KubernetesConstants.FileBeatConfigMountPath,
                SubPath = KubernetesConstants.FileBeatConfigFileName
            }
        };
    }

    private static List<V1Volume> CreatePodTemplateVolumes(string configMapName, string sideCarConfigMapName)
    {
        return new List<V1Volume>
        {
            new V1Volume
            {
                Name = "config-volume",
                ConfigMap = new V1ConfigMapVolumeSource
                {
                    Name = configMapName,
                    Items = new List<V1KeyToPath>
                    {
                        new V1KeyToPath
                        {
                            Key = KubernetesConstants.AppSettingFileName,
                            Path = KubernetesConstants.AppSettingFileName
                        },
                        new V1KeyToPath
                        {
                            Key = KubernetesConstants.AppSettingSharedFileName,
                            Path = KubernetesConstants.AppSettingSharedFileName
                        },
                        new V1KeyToPath
                        {
                            Key = KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                            Path = KubernetesConstants.AppSettingHttpApiHostSharedFileName
                        },
                        new V1KeyToPath
                        {
                            Key = KubernetesConstants.AppSettingSiloSharedFileName,
                            Path = KubernetesConstants.AppSettingSiloSharedFileName
                        },
                        new V1KeyToPath
                        {
                            Key = SecureConfigurationExtensions.DefaultBusinessConfigPath,
                            Path = SecureConfigurationExtensions.DefaultBusinessConfigPath
                        }
                    }
                }
            },
            new V1Volume
            {
                Name = "sidecar-config-volume",
                ConfigMap = new V1ConfigMapVolumeSource
                {
                    Name = sideCarConfigMapName,
                    Items = new List<V1KeyToPath>
                    {
                        new V1KeyToPath
                        {
                            Key = KubernetesConstants.FileBeatConfigFileName,
                            Path = KubernetesConstants.FileBeatConfigFileName
                        }
                    }
                }
            },
            new V1Volume
            {
                Name = "log-volume",
                EmptyDir = new V1EmptyDirVolumeSource()
                // PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                // {
                //     ClaimName = logPVCName
                // }
            }
        };
    }
}