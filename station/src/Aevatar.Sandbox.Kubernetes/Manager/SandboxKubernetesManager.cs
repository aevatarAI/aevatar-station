using System.Text;
using Aevatar.Sandbox.Kubernetes.Adapter;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Aevatar.Sandbox.Kubernetes.Manager;

public class SandboxKubernetesManager : ISandboxKubernetesManager
{
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly ILogger<SandboxKubernetesManager> _logger;
    private const string SandboxNamespace = "sandbox";

    public SandboxKubernetesManager(
        IKubernetesClientAdapter kubernetesClientAdapter,
        ILogger<SandboxKubernetesManager> logger)
    {
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _logger = logger;
    }

    public async Task<V1Job> CreateJobAsync(SandboxJobSpec spec, CancellationToken ct = default)
    {
        var job = new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"sandbox-{spec.SandboxExecutionId}",
                NamespaceProperty = SandboxNamespace,
                Labels = new Dictionary<string, string>
                {
                    ["app"] = "sandbox",
                    ["sandbox-execution-id"] = spec.SandboxExecutionId
                }
            },
            Spec = new V1JobSpec
            {
                BackoffLimit = 0, // No retries
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            ["app"] = "sandbox",
                            ["sandbox-execution-id"] = spec.SandboxExecutionId
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        SecurityContext = new V1PodSecurityContext
                        {
                            RunAsNonRoot = true,
                            RunAsUser = 1000,
                            RunAsGroup = 1000,
                            FsGroup = 1000
                        },
                        Containers = new List<V1Container>
                        {
                            new()
                            {
                                Name = "sandbox",
                                Image = spec.Image,
                                Command = spec.Command.ToList(),
                                Env = spec.Environment.Select(kvp => new V1EnvVar
                                {
                                    Name = kvp.Key,
                                    Value = kvp.Value
                                }).ToList(),
                                Resources = new V1ResourceRequirements
                                {
                                    Limits = new Dictionary<string, ResourceQuantity>
                                    {
                                        ["cpu"] = new ResourceQuantity($"{spec.ResourceLimits.CpuMillicores}m"),
                                        ["memory"] = new ResourceQuantity($"{spec.ResourceLimits.MemoryMB}Mi")
                                    },
                                    Requests = new Dictionary<string, ResourceQuantity>
                                    {
                                        ["cpu"] = new ResourceQuantity($"{spec.ResourceLimits.CpuMillicores/2}m"),
                                        ["memory"] = new ResourceQuantity($"{spec.ResourceLimits.MemoryMB/2}Mi")
                                    }
                                },
                                SecurityContext = new V1SecurityContext
                                {
                                    AllowPrivilegeEscalation = false,
                                    ReadOnlyRootFilesystem = true,
                                    Capabilities = new V1Capabilities
                                    {
                                        Drop = new List<string> { "ALL" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Add network policy if needed
        if (!spec.NetworkPolicy.AllowEgress)
        {
            var networkPolicy = new V1NetworkPolicy
            {
                Metadata = new V1ObjectMeta
                {
                    Name = $"sandbox-{spec.SandboxExecutionId}-network",
                    NamespaceProperty = SandboxNamespace
                },
                Spec = new V1NetworkPolicySpec
                {
                    PodSelector = new V1LabelSelector
                    {
                        MatchLabels = new Dictionary<string, string>
                        {
                            ["sandbox-execution-id"] = spec.SandboxExecutionId
                        }
                    },
                    PolicyTypes = new List<string> { "Egress" },
                    Egress = new List<V1NetworkPolicyEgressRule>()
                }
            };

            if (spec.NetworkPolicy.AllowedHosts?.Any() == true)
            {
                networkPolicy.Spec.Egress.Add(new V1NetworkPolicyEgressRule
                {
                    To = spec.NetworkPolicy.AllowedHosts.Select(host => new V1NetworkPolicyPeer
                    {
                        IpBlock = new V1IPBlock
                        {
                            Cidr = host
                        }
                    }).ToList()
                });
            }

            await _kubernetesClientAdapter.CreateNetworkPolicyAsync(networkPolicy, SandboxNamespace, ct);
        }

        return await _kubernetesClientAdapter.CreateJobAsync(job, SandboxNamespace, ct);
    }

    public async Task<SandboxJobStatus> GetJobStatusAsync(string sandboxExecutionId, CancellationToken ct = default)
    {
        var jobName = $"sandbox-{sandboxExecutionId}";
        var job = await _kubernetesClientAdapter.ReadNamespacedJobAsync(jobName, SandboxNamespace, ct);
        
        if (job == null)
            throw new InvalidOperationException($"Job {jobName} not found");

        var status = job.Status;
        var isComplete = status.CompletionTime.HasValue;
        var exitCode = 0;
        var timedOut = false;

        if (isComplete && status.Conditions != null)
        {
            var failedCondition = status.Conditions.FirstOrDefault(c => c.Type == "Failed" && c.Status == "True");
            if (failedCondition != null)
            {
                exitCode = 1;
                timedOut = failedCondition.Reason == "DeadlineExceeded";
            }
        }

        // Get pod to check resource usage
        var pods = await _kubernetesClientAdapter.ListNamespacedPodAsync(SandboxNamespace, ct);
        var pod = pods.Items.FirstOrDefault(p => p.Metadata.Labels["sandbox-execution-id"] == sandboxExecutionId);
        
        var memoryUsedMB = 0;
        var executionTimeSeconds = 0.0;
        
        if (pod != null && pod.Status.ContainerStatuses != null)
        {
            var containerStatus = pod.Status.ContainerStatuses.FirstOrDefault();
            if (containerStatus?.State?.Terminated != null)
            {
                exitCode = containerStatus.State.Terminated.ExitCode;
                
                if (containerStatus.State.Terminated.StartedAt.HasValue && containerStatus.State.Terminated.FinishedAt.HasValue)
                {
                    executionTimeSeconds = (containerStatus.State.Terminated.FinishedAt.Value - containerStatus.State.Terminated.StartedAt.Value).TotalSeconds;
                }
            }
        }

        return new SandboxJobStatus
        {
            IsComplete = isComplete,
            ExitCode = exitCode,
            TimedOut = timedOut,
            ExecutionTimeSeconds = executionTimeSeconds,
            MemoryUsedMB = memoryUsedMB,
            ScriptHash = job.Metadata.Labels.TryGetValue("script-hash", out var hash) ? hash : string.Empty,
            FinishedAtUtc = status.CompletionTime?.ToUniversalTime() ?? DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteJobAsync(string sandboxExecutionId, CancellationToken ct = default)
    {
        try
        {
            var jobName = $"sandbox-{sandboxExecutionId}";
            await _kubernetesClientAdapter.DeleteJobAsync(jobName, SandboxNamespace, ct);

            // Also try to delete the network policy if it exists
            try
            {
                var networkPolicyName = $"sandbox-{sandboxExecutionId}-network";
                await _kubernetesClientAdapter.DeleteNetworkPolicyAsync(networkPolicyName, SandboxNamespace, ct);
            }
            catch
            {
                // Ignore network policy deletion errors
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sandbox job {SandboxExecutionId}", sandboxExecutionId);
            return false;
        }
    }

    public async Task<SandboxJobLogs> GetJobLogsAsync(string sandboxExecutionId, int? maxLines = null, bool includeStderr = true, string? since = null, CancellationToken ct = default)
    {
        var pods = await _kubernetesClientAdapter.ListNamespacedPodAsync(SandboxNamespace, ct);
        var pod = pods.Items.FirstOrDefault(p => p.Metadata.Labels["sandbox-execution-id"] == sandboxExecutionId);
        
        if (pod == null)
            throw new InvalidOperationException($"Pod for sandbox execution {sandboxExecutionId} not found");

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var truncated = false;

        // Get stdout
        try
        {
            using var stdoutStream = await _kubernetesClientAdapter.ReadNamespacedPodLogAsync(
                pod.Metadata.Name,
                SandboxNamespace,
                container: "sandbox",
                follow: false,
                tailLines: maxLines,
                sinceSeconds: since != null ? (int?)double.Parse(since) : null,
                ct: ct);

            using var reader = new StreamReader(stdoutStream);
            stdout.Append(await reader.ReadToEndAsync(ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stdout logs for sandbox execution {SandboxExecutionId}", sandboxExecutionId);
        }

        // Get stderr if requested
        if (includeStderr)
        {
            try
            {
                using var stderrStream = await _kubernetesClientAdapter.ReadNamespacedPodLogAsync(
                    pod.Metadata.Name,
                    SandboxNamespace,
                    container: "sandbox",
                    follow: false,
                    tailLines: maxLines,
                    sinceSeconds: since != null ? (int?)double.Parse(since) : null,
                    ct: ct);

                using var reader = new StreamReader(stderrStream);
                stderr.Append(await reader.ReadToEndAsync(ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stderr logs for sandbox execution {SandboxExecutionId}", sandboxExecutionId);
            }
        }

        // Check if logs were truncated
        if (maxLines.HasValue)
        {
            truncated = stdout.Length >= maxLines.Value || stderr.Length >= maxLines.Value;
        }

        return new SandboxJobLogs
        {
            Stdout = stdout.ToString(),
            Stderr = stderr.ToString(),
            Truncated = truncated
        };
    }
}