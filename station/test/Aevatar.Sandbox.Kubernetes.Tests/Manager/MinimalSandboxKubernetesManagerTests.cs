using System.Text;
using Aevatar.Sandbox.Kubernetes.Adapter;
using Aevatar.Sandbox.Kubernetes.Manager;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Aevatar.Sandbox.Kubernetes.Manager;

/// <summary>
/// 最小化的SandboxKubernetesManager测试类
/// </summary>
public class MinimalSandboxKubernetesManagerTests
{
    private readonly Mock<ISandboxKubernetesClientAdapter> _mockClientAdapter;
    private readonly Mock<ILogger<SandboxKubernetesManager>> _mockLogger;
    private readonly SandboxKubernetesManager _manager;

    public MinimalSandboxKubernetesManagerTests()
    {
        _mockClientAdapter = new Mock<ISandboxKubernetesClientAdapter>();
        _mockLogger = new Mock<ILogger<SandboxKubernetesManager>>();
        _manager = new SandboxKubernetesManager(_mockClientAdapter.Object, _mockLogger.Object);
    }

    [Fact]
    public void Manager_Should_Be_Created()
    {
        // 简单验证管理器是否被正确创建
        _manager.ShouldNotBeNull();
    }

    [Fact]
    public void Manager_Should_Implement_ISandboxKubernetesManager()
    {
        // 验证管理器是否实现了正确的接口
        _manager.ShouldBeAssignableTo<ISandboxKubernetesManager>();
    }

    [Fact]
    public async Task CreateJobAsync_Should_Call_Adapter()
    {
        // Arrange
        var executionId = "test-execution-id";
        var spec = new SandboxJobSpec
        {
            SandboxExecutionId = executionId,
            Image = "test-image",
            Command = new[] { "python", "-c", "print('Hello, World!')" }
        };

        var expectedJob = new V1Job
        {
            Metadata = new V1ObjectMeta { Name = $"sandbox-{executionId}" }
        };

        _mockClientAdapter
            .Setup(x => x.CreateJobAsync(It.IsAny<V1Job>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedJob);

        _mockClientAdapter
            .Setup(x => x.CreateNetworkPolicyAsync(It.IsAny<V1NetworkPolicy>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1NetworkPolicy());

        // Act
        var result = await _manager.CreateJobAsync(spec);

        // Assert
        result.ShouldNotBeNull();
        result.Metadata.Name.ShouldBe($"sandbox-{executionId}");
        
        // 验证适配器方法是否被调用
        _mockClientAdapter.Verify(
            x => x.CreateJobAsync(It.IsAny<V1Job>(), "sandbox", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}