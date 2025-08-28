using System.Threading.Tasks;

namespace Aevatar.Kubernetes.Abstractions;

public interface IKubernetesHostManager
{
    Task<KubernetesJobResult> RunJobAsync(KubernetesJobOptions options);
    Task<KubernetesJobResult> GetJobStatusAsync(string jobName, string @namespace);
    Task<KubernetesLogResult> GetJobLogsAsync(string jobName, string @namespace, LogOptions options);
    Task DeleteJobAsync(string jobName, string @namespace);
}