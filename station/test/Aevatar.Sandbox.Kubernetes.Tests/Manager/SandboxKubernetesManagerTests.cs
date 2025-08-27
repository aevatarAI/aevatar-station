using System.Text;
using Aevatar.Sandbox.Kubernetes.Adapter;
using Aevatar.Sandbox.Kubernetes.Manager;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Aevatar.Sandbox.Kubernetes.Manager;

public class SandboxKubernetesManagerTests
{
    private readonly Mock<ISandboxKubernetesClientAdapter> _mockClientAdapter;
    private readonly Mock<ILogger<SandboxKubernetesManager>> _mockLogger;
    private readonly SandboxKubernetesManager _manager;

    public SandboxKubernetesManagerTests()
    {
        _mockClientAdapter = new Mock<ISandboxKubernetesClientAdapter>();
        _mockLogger = new Mock<ILogger<SandboxKubernetesManager>>();
        _manager = new SandboxKubernetesManager(_mockClientAdapter.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateJobAsync_Should_Create_Job_With_Correct_Parameters()
    {
        // Arrange
        var executionId = "test-execution-id";
        var spec = new SandboxJobSpec
        {
            SandboxExecutionId = executionId,
            Image = "test-image",
            Command = new[] { "python", "-c", "print('Hello, World!')" },
            Environment = new Dictionary<string, string>
            {
                ["TEST_VAR"] = "test-value"
            },
            ResourceLimits = new SandboxResourceLimits
            {
                CpuMillicores = 500,
                MemoryMB = 256,
                TimeoutSeconds = 60
            },
            NetworkPolicy = new NetworkPolicy
            {
                AllowEgress = false, // 修改为false，这样才会创建NetworkPolicy
                AllowedHosts = new[] { "example.com" }
            }
        };

        var createdJob = new V1Job
        {
            Metadata = new V1ObjectMeta { Name = $"sandbox-{executionId}" }
        };

        _mockClientAdapter.Setup(x => x.CreateJobAsync(
                It.IsAny<V1Job>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdJob);

        _mockClientAdapter.Setup(x => x.CreateNetworkPolicyAsync(
                It.IsAny<V1NetworkPolicy>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1NetworkPolicy());

        // Act
        var result = await _manager.CreateJobAsync(spec);

        // Assert
        result.ShouldNotBeNull();
        result.Metadata.Name.ShouldBe($"sandbox-{executionId}");

        _mockClientAdapter.Verify(x => x.CreateJobAsync(
            It.Is<V1Job>(j => 
                j.Metadata.Name == $"sandbox-{executionId}" &&
                j.Metadata.NamespaceProperty == "sandbox" &&
                j.Metadata.Labels["sandbox-execution-id"] == executionId),
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _mockClientAdapter.Verify(x => x.CreateNetworkPolicyAsync(
            It.Is<V1NetworkPolicy>(np => 
                np.Metadata.Name.StartsWith($"sandbox-{executionId}") &&
                np.Metadata.NamespaceProperty == "sandbox"),
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetJobStatusAsync_Should_Return_Job_Status()
    {
        // Arrange
        var executionId = "test-execution-id";
        var jobName = $"sandbox-{executionId}";
        
        var job = new V1Job
        {
            Metadata = new V1ObjectMeta 
            { 
                Name = jobName,
                CreationTimestamp = DateTime.UtcNow.AddMinutes(-1),
                Labels = new Dictionary<string, string>() // 添加Labels字典，避免空引用
            },
            Status = new V1JobStatus
            {
                Succeeded = 1,
                CompletionTime = DateTime.UtcNow,
                Conditions = new List<V1JobCondition>() // 添加Conditions列表，避免空引用
            }
        };

        var podList = new V1PodList
        {
            Items = new List<V1Pod>
            {
                new V1Pod
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"{jobName}-pod",
                        Labels = new Dictionary<string, string>
                        {
                            ["sandbox-execution-id"] = executionId
                        }
                    },
                    Status = new V1PodStatus
                    {
                        ContainerStatuses = new List<V1ContainerStatus>
                        {
                            new V1ContainerStatus
                            {
                                Name = "sandbox",
                                State = new V1ContainerState
                                {
                                    Terminated = new V1ContainerStateTerminated
                                    {
                                        ExitCode = 0,
                                        Reason = "Completed",
                                        StartedAt = DateTime.UtcNow.AddMinutes(-1), // 添加开始时间
                                        FinishedAt = DateTime.UtcNow // 添加结束时间
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        _mockClientAdapter.Setup(x => x.ReadNamespacedJobAsync(
                jobName,
                "sandbox",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _mockClientAdapter.Setup(x => x.ListNamespacedPodAsync(
                "sandbox",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(podList);

        // Act
        var result = await _manager.GetJobStatusAsync(executionId);

        // Assert
        result.ShouldNotBeNull();
        result.IsComplete.ShouldBeTrue();
        result.ExitCode.ShouldBe(0);
        result.TimedOut.ShouldBeFalse();
        
        _mockClientAdapter.Verify(x => x.ReadNamespacedJobAsync(
            jobName,
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _mockClientAdapter.Verify(x => x.ListNamespacedPodAsync(
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task DeleteJobAsync_Should_Delete_Job_And_Network_Policy()
    {
        // Arrange
        var executionId = "test-execution-id";
        var jobName = $"sandbox-{executionId}";
        var networkPolicyName = $"sandbox-{executionId}-network-policy";

        _mockClientAdapter.Setup(x => x.DeleteJobAsync(
                jobName,
                "sandbox",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClientAdapter.Setup(x => x.DeleteNetworkPolicyAsync(
                It.IsAny<string>(),
                "sandbox",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _manager.DeleteJobAsync(executionId);

        // Assert
        result.ShouldBeTrue();
        
        _mockClientAdapter.Verify(x => x.DeleteJobAsync(
            jobName,
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _mockClientAdapter.Verify(x => x.DeleteNetworkPolicyAsync(
            It.Is<string>(name => name.StartsWith($"sandbox-{executionId}")),
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetJobLogsAsync_Should_Return_Job_Logs()
    {
        // Arrange
        var executionId = "test-execution-id";
        var jobName = $"sandbox-{executionId}";
        var podName = $"{jobName}-pod";
        var maxLines = 100;
        var includeStderr = false; // 修改为false，这样就不会调用两次ReadNamespacedPodLogAsync
        
        var podList = new V1PodList
        {
            Items = new List<V1Pod>
            {
                new V1Pod
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = podName,
                        Labels = new Dictionary<string, string>
                        {
                            ["sandbox-execution-id"] = executionId
                        }
                    }
                }
            }
        };

        var stdoutContent = "Hello, World!";
        var stdoutStream = new MemoryStream(Encoding.UTF8.GetBytes(stdoutContent));

        _mockClientAdapter.Setup(x => x.ListNamespacedPodAsync(
                "sandbox",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(podList);

        _mockClientAdapter.Setup(x => x.ReadNamespacedPodLogAsync(
                podName,
                "sandbox",
                "sandbox",
                false,
                maxLines,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stdoutStream);

        // Act
        var result = await _manager.GetJobLogsAsync(executionId, maxLines, includeStderr);

        // Assert
        result.ShouldNotBeNull();
        result.Stdout.ShouldBe(stdoutContent);
        
        _mockClientAdapter.Verify(x => x.ListNamespacedPodAsync(
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _mockClientAdapter.Verify(x => x.ReadNamespacedPodLogAsync(
            podName,
            "sandbox",
            "sandbox",
            false,
            maxLines,
            null,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetJobStatusAsync_Should_Return_NotFound_Status_When_Job_Not_Found()
    {
        // Arrange
        var executionId = "non-existent-id";
        var jobName = $"sandbox-{executionId}";

        _mockClientAdapter.Setup(x => x.ReadNamespacedJobAsync(
                jobName,
                "sandbox",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Job)null); // 返回null而不是抛出异常

        // Act
        var result = await _manager.GetJobStatusAsync(executionId);

        // Assert
        result.ShouldNotBeNull();
        result.IsComplete.ShouldBeFalse();
        
        _mockClientAdapter.Verify(x => x.ReadNamespacedJobAsync(
            jobName,
            "sandbox",
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}