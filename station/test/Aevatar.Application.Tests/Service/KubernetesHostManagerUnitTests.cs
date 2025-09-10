// ABOUTME: This file contains isolated unit tests for KubernetesHostManager CopyDeploymentWithPatternAsync functionality
// ABOUTME: Tests validate deployment copying logic with pattern modifications based on clone_deployment.sh script

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.Manager;
using Aevatar.Options;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;
using Aevatar.Application.Grains.Agents.Configuration;
using Orleans;
using System.Linq;
using System.Threading;

namespace Aevatar.Application.Tests.Service;

public class KubernetesHostManagerUnitTests
{
    private readonly Mock<ILogger<KubernetesHostManager>> _mockLogger;
    private readonly Mock<IKubernetesClientAdapter> _mockKubernetesClientAdapter;
    private readonly Mock<IOptionsSnapshot<KubernetesOptions>> _mockKubernetesOptions;
    private readonly Mock<IOptionsSnapshot<HostDeployOptions>> _mockHostDeployOptions;
    private readonly Mock<IGrainFactory> _mockGrainFactory;

    public KubernetesHostManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<KubernetesHostManager>>();
        _mockKubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        _mockKubernetesOptions = new Mock<IOptionsSnapshot<KubernetesOptions>>();
        _mockHostDeployOptions = new Mock<IOptionsSnapshot<HostDeployOptions>>();
        _mockGrainFactory = new Mock<IGrainFactory>();

        // Setup default values
        _mockKubernetesOptions.Setup(x => x.Value).Returns(new KubernetesOptions
        {
            AppPodReplicas = 1,
            RequestCpuCore = "100m",
            RequestMemory = "128Mi"
        });

        _mockHostDeployOptions.Setup(x => x.Value).Returns(new HostDeployOptions
        {
            HostSiloImageName = "test-silo-image:latest"
        });

        // Initialize KubernetesConstants for testing
        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfiguration.Setup(x => x["Kubernetes:AppNameSpace"]).Returns("test-namespace");
        KubernetesConstants.Initialize(mockConfiguration.Object);
    }

    private KubernetesHostManager CreateKubernetesHostManager()
    {
        return new KubernetesHostManager(
            _mockLogger.Object,
            _mockKubernetesClientAdapter.Object,
            _mockKubernetesOptions.Object,
            _mockHostDeployOptions.Object,
            _mockGrainFactory.Object
        );
    }

    [Fact]
    public async Task CopyDeploymentWithPatternAsync_Should_Copy_Deployment_Successfully()
    {
        // Arrange
        var hostManager = CreateKubernetesHostManager();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        var sourceDeployment = CreateMockDeployment("deployment-testclient-silo-1", "container-testclient-silo-1");
        
        _mockKubernetesClientAdapter
            .Setup(x => x.ReadNamespacedDeploymentAsync("deployment-testclient-silo-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceDeployment);

        _mockKubernetesClientAdapter
            .Setup(x => x.CreateDeploymentAsync(It.IsAny<V1Deployment>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1Deployment());

        // Act
        await hostManager.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern);

        // Assert
        _mockKubernetesClientAdapter.Verify(
            x => x.ReadNamespacedDeploymentAsync("deployment-testclient-silo-1", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockKubernetesClientAdapter.Verify(
            x => x.CreateDeploymentAsync(It.Is<V1Deployment>(d => 
                d.Metadata.Name == "deployment-testclient-silo-2" &&
                d.Spec.Template.Spec.Containers.Any(c => 
                    c.Env.Any(e => e.Name == "SILO_NAME_PATTERN" && e.Value == siloNamePattern))),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CopyDeploymentWithPatternAsync_Should_Throw_When_Source_Not_Found()
    {
        // Arrange
        var hostManager = CreateKubernetesHostManager();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        _mockKubernetesClientAdapter
            .Setup(x => x.ReadNamespacedDeploymentAsync("deployment-testclient-silo-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Deployment)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            hostManager.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern));

        exception.Message.ShouldContain("Source deployment deployment-testclient-silo-1 not found");
    }

    [Fact]
    public async Task CopyDeploymentWithPatternAsync_Should_Call_ReadNamespacedDeploymentAsync_With_Correct_Parameters()
    {
        // Arrange
        var hostManager = CreateKubernetesHostManager();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        var sourceDeployment = CreateMockDeployment("deployment-testclient-silo-1", "container-testclient-silo-1");
        
        _mockKubernetesClientAdapter
            .Setup(x => x.ReadNamespacedDeploymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceDeployment);

        _mockKubernetesClientAdapter
            .Setup(x => x.CreateDeploymentAsync(It.IsAny<V1Deployment>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1Deployment());

        // Act
        await hostManager.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern);

        // Assert
        _mockKubernetesClientAdapter.Verify(
            x => x.ReadNamespacedDeploymentAsync("deployment-testclient-silo-1", "test-namespace", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CopyDeploymentWithPatternAsync_Should_Call_CreateDeploymentAsync_With_Correct_Parameters()
    {
        // Arrange
        var hostManager = CreateKubernetesHostManager();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        var sourceDeployment = CreateMockDeployment("deployment-testclient-silo-1", "container-testclient-silo-1");
        
        _mockKubernetesClientAdapter
            .Setup(x => x.ReadNamespacedDeploymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceDeployment);

        _mockKubernetesClientAdapter
            .Setup(x => x.CreateDeploymentAsync(It.IsAny<V1Deployment>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1Deployment());

        // Act
        await hostManager.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern);

        // Assert
        _mockKubernetesClientAdapter.Verify(
            x => x.CreateDeploymentAsync(It.IsAny<V1Deployment>(), "test-namespace", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CopyDeploymentWithPatternAsync_Should_Throw_Exception_When_Source_Deployment_Not_Found()
    {
        // Arrange
        var hostManager = CreateKubernetesHostManager();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        _mockKubernetesClientAdapter
            .Setup(x => x.ReadNamespacedDeploymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Deployment)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => hostManager.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern));
        
        exception.Message.ShouldContain("deployment-testclient-silo-1");
        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task CopyDeploymentWithPatternAsync_Should_Handle_Kubernetes_Exception()
    {
        // Arrange
        var hostManager = CreateKubernetesHostManager();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        _mockKubernetesClientAdapter
            .Setup(x => x.ReadNamespacedDeploymentAsync("deployment-testclient-silo-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Kubernetes API error"));

        // Act & Assert
        var exception = await Should.ThrowAsync<Exception>(() =>
            hostManager.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern));

        exception.Message.ShouldBe("Kubernetes API error");
    }

    private V1Deployment CreateMockDeployment(string deploymentName, string containerName, bool includeSiloNamePattern = true)
    {
        var envVars = new List<V1EnvVar>
        {
            new V1EnvVar("ORLEANS_SERVICE_ID", "testservice"),
            new V1EnvVar("ORLEANS_CLUSTER_ID", "testcluster")
        };

        if (includeSiloNamePattern)
        {
            envVars.Add(new V1EnvVar("SILO_NAME_PATTERN", "Projector"));
        }

        return new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = deploymentName,
                Labels = new Dictionary<string, string>
                {
                    { "app", deploymentName },
                    { "version", "1" }
                }
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = 1,
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { "app", deploymentName }
                    }
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            { "app", deploymentName }
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = containerName,
                                Image = "test-image:latest",
                                Env = envVars
                            }
                        }
                    }
                }
            }
        };
    }
}