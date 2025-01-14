using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class PersistentVolumeClaimHelper
{
    public static V1PersistentVolumeClaim CreatePersistentVolumeClaimDefinition(string pvcName, string nameSpace,
        string storageResourceQuantity)
    {
        // Define a PersistentVolumeClaim
        var pvc = new V1PersistentVolumeClaim
        {
            Metadata = new V1ObjectMeta
            {
                Name = pvcName,
                NamespaceProperty = nameSpace
            },
            Spec = new V1PersistentVolumeClaimSpec
            {
                AccessModes = new List<string> { "ReadWriteMany" },
                Resources = new V1VolumeResourceRequirements()
                {
                    Requests = new Dictionary<string, ResourceQuantity>
                    {
                        // { "storage", new ResourceQuantity("1Gi") }
                        { "storage", new ResourceQuantity(storageResourceQuantity) }
                    }
                },
                StorageClassName = "standard" // 存储类名称
            }
        };

        return pvc;
    }
}