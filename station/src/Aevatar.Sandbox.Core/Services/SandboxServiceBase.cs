using System;
using System.Threading.Tasks;
using Aevatar.Kubernetes.Abstractions;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Sandbox.Core.Services;

public abstract class SandboxServiceBase : ISandboxService
{
    protected readonly ILogger Logger;
    protected readonly IKubernetesHostManager KubernetesManager;
    protected readonly SandboxOptions Options;
    protected readonly SandboxExecDispatcher Dispatcher;

    protected SandboxServiceBase(
        ILogger logger,
        IKubernetesHostManager kubernetesManager,
        IOptions<SandboxOptions> options,
        SandboxExecDispatcher dispatcher)
    {
        Logger = logger;
        KubernetesManager = kubernetesManager;
        Options = options.Value;
        Dispatcher = dispatcher;
    }

    protected abstract string GetImage();
    protected abstract string GetNamespace();
    protected abstract string GetLanguage();
    protected abstract string[] GetCommand(string code);

    public async Task<SandboxExecutionResult> ExecuteAsync(string code, int timeout, ResourceLimits resources)
    {
        var executionId = Guid.NewGuid().ToString("N");
        var jobOptions = new KubernetesJobOptions
        {
            JobName = $"sandbox-{GetLanguage()}-{executionId}",
            Namespace = GetNamespace(),
            Image = GetImage(),
            Command = GetCommand(code),
            TimeoutSeconds = timeout,
            CpuLimit = $"{resources.CpuLimitCores}",
            MemoryLimit = $"{resources.MemoryLimitBytes}M"
        };

        var result = await KubernetesManager.RunJobAsync(jobOptions);
        return new SandboxExecutionResult
        {
            ExecutionId = executionId,
            Status = result.Status == "Succeeded" ? ExecutionStatus.Completed : ExecutionStatus.Failed,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            Language = GetLanguage(),
            PodName = jobOptions.JobName,
            ResourceUsage = new ResourceUsage
            {
                CpuUsageCores = double.Parse(result.CpuUsage.TrimEnd('m')) / 1000,
                MemoryUsageBytes = long.Parse(result.MemoryUsage.TrimEnd('M')) * 1024 * 1024,
                NetworkInBytes = long.Parse(result.NetworkIn),
                NetworkOutBytes = long.Parse(result.NetworkOut),
                DiskReadBytes = long.Parse(result.DiskRead),
                DiskWriteBytes = long.Parse(result.DiskWrite)
            },
            ExitCode = result.ExitCode,
            Output = result.Output,
            Error = result.Error
        };
    }

    public async Task<SandboxLogs> GetLogsAsync(string executionId, LogQueryOptions? options = null)
    {
        var jobName = $"sandbox-{GetLanguage()}-{executionId}";
        var result = await KubernetesManager.GetJobLogsAsync(jobName, GetNamespace(), new LogOptions
        {
            MaxLines = options?.MaxLines ?? 1000,
            Tail = options?.Tail ?? true,
            Since = options?.Since,
            Until = options?.Until,
            Follow = options?.Follow ?? false
        });

        return new SandboxLogs
        {
            ExecutionId = executionId,
            PodName = jobName,
            Namespace = GetNamespace(),
            Lines = result.Lines,
            HasMore = result.HasMore,
            Error = result.Error
        };
    }

    public async Task CancelAsync(string executionId)
    {
        var jobName = $"sandbox-{GetLanguage()}-{executionId}";
        await KubernetesManager.DeleteJobAsync(jobName, GetNamespace());
    }

    public async Task<SandboxExecutionResult> GetStatusAsync(string executionId)
    {
        var jobName = $"sandbox-{GetLanguage()}-{executionId}";
        var result = await KubernetesManager.GetJobStatusAsync(jobName, GetNamespace());
        return new SandboxExecutionResult
        {
            ExecutionId = executionId,
            Status = result.Status == "Succeeded" ? ExecutionStatus.Completed : ExecutionStatus.Failed,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            Language = GetLanguage(),
            PodName = jobName,
            ResourceUsage = new ResourceUsage
            {
                CpuUsageCores = double.Parse(result.CpuUsage.TrimEnd('m')) / 1000,
                MemoryUsageBytes = long.Parse(result.MemoryUsage.TrimEnd('M')) * 1024 * 1024,
                NetworkInBytes = long.Parse(result.NetworkIn),
                NetworkOutBytes = long.Parse(result.NetworkOut),
                DiskReadBytes = long.Parse(result.DiskRead),
                DiskWriteBytes = long.Parse(result.DiskWrite)
            },
            ExitCode = result.ExitCode,
            Output = result.Output,
            Error = result.Error
        };
    }
}