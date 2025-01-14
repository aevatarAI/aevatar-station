using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class PersistentVolumeHelper
{
    /// <summary>
    /// Create a pv with nfs
    /// </summary>
    /// <param name="pvName"></param>
    /// <param name="storageResourceQuantity"></param>
    /// <param name="nfsShardPath"></param>
    /// <param name="nfsHostAddress"></param>
    /// <returns></returns>
    public static V1PersistentVolume CreatePersistentVolumeWithNFSDefinition(string pvName,
        string storageResourceQuantity, string nfsShardPath, string nfsHostAddress)
    {
        // Define the PersistentVolume specification
        var pv = new V1PersistentVolume
        {
            Metadata = new V1ObjectMeta
            {
                Name = pvName, // Name of the PV
            },
            Spec = new V1PersistentVolumeSpec
            {
                Capacity = new Dictionary<string, ResourceQuantity>
                {
                    {
                        "storage", new ResourceQuantity(storageResourceQuantity)
                    } // Storage capacity should match the PVC's request
                },
                AccessModes = new List<string> { "ReadWriteMany" }, // Access modes should match the PVC's request
                PersistentVolumeReclaimPolicy = "Retain", // Reclaim policy
                StorageClassName = "standard", // The StorageClass should match the PVC's request
                // Configure the specific storage backend details, such as NFS
                Nfs = new V1NFSVolumeSource
                {
                    Path = nfsShardPath, // Path on the NFS server
                    Server = nfsHostAddress // Address of the NFS server
                },
                // If your PVC has a selector with match labels, you need to set them here
                // Selector = new V1LabelSelector
                // {
                //     MatchLabels = new Dictionary<string, string>
                //     {
                //         { "key", "value" } // The key-value pairs should match the PVC's selector
                //     }
                // }
            }
        };

        return pv;
    }
}