using Aevatar.Kubernetes.Abstractions;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Sandbox.Python.Services;

public class PythonSandboxServiceTests
{
    private readonly Mock<ILogger<PythonSandboxService>> _mockLogger;
    private readonly Mock<IKubernetesHostManager> _mockKubernetesManager;
    private readonly Mock<IOptions<SandboxOptions>> _mockOptions;
    private readonly Mock<IOptions<PythonSandboxOptions>> _mockPythonOptions;
    private readonly Mock<IGrainFactory> _mockGrainFactory;
    private readonly SandboxExecDispatcher _dispatcher;
    private readonly PythonSandboxService _service;

    public PythonSandboxServiceTests()
    {
        _mockLogger = new Mock<ILogger<PythonSandboxService>>();
        _mockKubernetesManager = new Mock<IKubernetesHostManager>();
        _mockOptions = new Mock<IOptions<SandboxOptions>>();
        _mockPythonOptions = new Mock<IOptions<PythonSandboxOptions>>();
        _mockGrainFactory = new Mock<IGrainFactory>();
        
        // 创建真实的SandboxDispatcherOptions
        var dispatcherOptions = new Mock<IOptions<SandboxDispatcherOptions>>();
        dispatcherOptions.Setup(x => x.Value).Returns(new SandboxDispatcherOptions
        {
            MaxConcurrentExecutionsPerLanguage = 10,
            QueueTimeout = TimeSpan.FromSeconds(30),
            ExecutionTimeout = 30
        });
        
        // 创建真实的SandboxExecDispatcher
        _dispatcher = new SandboxExecDispatcher(
            _mockGrainFactory.Object,
            Mock.Of<ILogger<SandboxExecDispatcher>>(),
            dispatcherOptions.Object
        );

        // Setup default options
        var sandboxOptions = new SandboxOptions
        {
            DefaultTimeoutSeconds = 30,
            DefaultCpuLimit = 1.0,
            DefaultMemoryLimit = 512,
            DefaultNamespace = "sandbox"
        };
        _mockOptions.Setup(x => x.Value).Returns(sandboxOptions);

        var pythonOptions = new PythonSandboxOptions
        {
            PythonImage = "python:3.9-slim",
            Namespace = "sandbox-python"
        };
        _mockPythonOptions.Setup(x => x.Value).Returns(pythonOptions);

        _service = new PythonSandboxService(
            _mockLogger.Object,
            _mockKubernetesManager.Object,
            _mockOptions.Object,
            _mockPythonOptions.Object,
            _dispatcher);
    }

    [Fact]
    public void GetImage_Should_Return_PythonImage()
    {
        // Act
        var image = GetProtectedMethod<string>(_service, "GetImage");

        // Assert
        image.ShouldBe("python:3.9-slim");
    }

    [Fact]
    public void GetNamespace_Should_Return_Namespace()
    {
        // Act
        var ns = GetProtectedMethod<string>(_service, "GetNamespace");

        // Assert
        ns.ShouldBe("sandbox-python");
    }

    [Fact]
    public void GetLanguage_Should_Return_Python()
    {
        // Act
        var language = GetProtectedMethod<string>(_service, "GetLanguage");

        // Assert
        language.ShouldBe("python");
    }

    [Fact]
    public void GetCommand_Should_Return_PythonCommand()
    {
        // Arrange
        var code = "print('Hello, World!')";

        // Act
        var command = GetProtectedMethod<string[]>(_service, "GetCommand", code);

        // Assert
        command.ShouldNotBeNull();
        command.Length.ShouldBe(3);
        command[0].ShouldBe("python");
        command[1].ShouldBe("-c");
        command[2].ShouldBe(code);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Call_KubernetesManager_RunJobAsync()
    {
        // Arrange
        var code = "print('Hello, World!')";
        var timeout = 30;
        var resources = new ResourceLimits
        {
            CpuLimitCores = 1.0,
            MemoryLimitBytes = 512 * 1024 * 1024,
            TimeoutSeconds = 30
        };

        var jobResult = new KubernetesJobResult
        {
            Status = "Succeeded",
            StartTime = DateTime.UtcNow.AddSeconds(-5),
            EndTime = DateTime.UtcNow,
            CpuUsage = "500m",
            MemoryUsage = "256M",
            NetworkIn = "1024",
            NetworkOut = "2048",
            DiskRead = "4096",
            DiskWrite = "8192",
            ExitCode = 0,
            Output = "Hello, World!",
            Error = ""
        };

        _mockKubernetesManager
            .Setup(x => x.RunJobAsync(It.IsAny<KubernetesJobOptions>()))
            .ReturnsAsync(jobResult);

        // Act
        var result = await _service.ExecuteAsync(code, timeout, resources);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ExecutionStatus.Completed);
        result.Output.ShouldBe("Hello, World!");
        result.Language.ShouldBe("python");

        _mockKubernetesManager.Verify(
            x => x.RunJobAsync(It.Is<KubernetesJobOptions>(o =>
                o.JobName.StartsWith("sandbox-python-") &&
                o.Namespace == "sandbox-python" &&
                o.Image == "python:3.9-slim" &&
                o.Command.Length == 3 &&
                o.Command[0] == "python" &&
                o.Command[1] == "-c" &&
                o.Command[2] == code &&
                o.TimeoutSeconds == timeout &&
                o.CpuLimit == "1" &&
                o.MemoryLimit == "536870912M"
            )),
            Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_Should_Call_KubernetesManager_GetJobStatusAsync()
    {
        // Arrange
        var executionId = "test-execution-id";
        var jobResult = new KubernetesJobResult
        {
            Status = "Succeeded",
            StartTime = DateTime.UtcNow.AddSeconds(-5),
            EndTime = DateTime.UtcNow,
            CpuUsage = "500m",
            MemoryUsage = "256M",
            NetworkIn = "1024",
            NetworkOut = "2048",
            DiskRead = "4096",
            DiskWrite = "8192",
            ExitCode = 0,
            Output = "Hello, World!",
            Error = ""
        };

        _mockKubernetesManager
            .Setup(x => x.GetJobStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jobResult);

        // Act
        var result = await _service.GetStatusAsync(executionId);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ExecutionStatus.Completed);
        result.Output.ShouldBe("Hello, World!");
        result.ExecutionId.ShouldBe(executionId);

        _mockKubernetesManager.Verify(
            x => x.GetJobStatusAsync($"sandbox-python-{executionId}", "sandbox-python"),
            Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_Should_Call_KubernetesManager_GetJobLogsAsync()
    {
        // Arrange
        var executionId = "test-execution-id";
        var logOptions = new LogQueryOptions
        {
            MaxLines = 100,
            Tail = true,
            Since = "5m",
            Until = null,
            Follow = false
        };

        var logResult = new KubernetesLogResult
        {
            Lines = new[] { "Line 1", "Line 2", "Line 3" },
            HasMore = false,
            Error = null
        };

        _mockKubernetesManager
            .Setup(x => x.GetJobLogsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LogOptions>()))
            .ReturnsAsync(logResult);

        // Act
        var result = await _service.GetLogsAsync(executionId, logOptions);

        // Assert
        result.ShouldNotBeNull();
        result.Lines.ShouldBe(new[] { "Line 1", "Line 2", "Line 3" });
        result.HasMore.ShouldBeFalse();
        result.Error.ShouldBeNull();
        result.ExecutionId.ShouldBe(executionId);

        _mockKubernetesManager.Verify(
            x => x.GetJobLogsAsync(
                $"sandbox-python-{executionId}",
                "sandbox-python",
                It.Is<LogOptions>(o =>
                    o.MaxLines == logOptions.MaxLines &&
                    o.Tail == logOptions.Tail &&
                    o.Since == logOptions.Since &&
                    o.Until == logOptions.Until &&
                    o.Follow == logOptions.Follow
                )),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_Should_Call_KubernetesManager_DeleteJobAsync()
    {
        // Arrange
        var executionId = "test-execution-id";

        _mockKubernetesManager
            .Setup(x => x.DeleteJobAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CancelAsync(executionId);

        // Assert
        _mockKubernetesManager.Verify(
            x => x.DeleteJobAsync($"sandbox-python-{executionId}", "sandbox-python"),
            Times.Once);
    }

    /// <summary>
    /// Helper method to invoke protected methods using reflection
    /// </summary>
    private static TResult GetProtectedMethod<TResult>(object instance, string methodName, params object[] parameters)
    {
        var type = instance.GetType();
        var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found on type {type.FullName}");
        
        var result = method.Invoke(instance, parameters);
        if (result == null)
            throw new InvalidOperationException($"Method {methodName} returned null");
            
        return (TResult)result;
    }
}