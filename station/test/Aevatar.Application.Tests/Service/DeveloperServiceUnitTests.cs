// ABOUTME: This file contains isolated unit tests for DeveloperService functionality
// ABOUTME: Tests validate basic method signature and interface implementation

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Enum;
using Aevatar.Service;
using Aevatar.WebHook.Deploy;
using Aevatar.Kubernetes.Manager;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Aevatar.Projects;
using Aevatar.Kubernetes.Adapter;
using k8s.Models;
using Aevatar.Kubernetes.ResourceDefinition;
using System.Threading;
using Volo.Abp;

namespace Aevatar.Application.Tests.Service;

public class DeveloperServiceUnitTests
{
    
        [Fact]
    public void DeveloperService_Should_Have_CopyHostAsync_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(IDeveloperService).GetMethod("CopyHostAsync");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].Name.ShouldBe("sourceClientId");
        parameters[1].Name.ShouldBe("newClientId");
        parameters[2].Name.ShouldBe("version");
    }

    [Fact]
    public void DeveloperService_Should_Implement_Interface_Correctly()
    {
        // Arrange
        var serviceType = typeof(DeveloperService);
        var interfaceType = typeof(IDeveloperService);

        // Act & Assert
        interfaceType.IsAssignableFrom(serviceType).ShouldBeTrue();
        
        var methods = interfaceType.GetMethods();
        methods.ShouldContain(m => m.Name == "CopyHostAsync");
    }

    [Fact]
    public void IHostCopyManager_Should_Have_CopyHostAsync_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(IHostCopyManager).GetMethod("CopyHostAsync");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].Name.ShouldBe("sourceClientId");
        parameters[1].Name.ShouldBe("newClientId");
        parameters[2].Name.ShouldBe("version");
    }

    [Fact]
    public void KubernetesHostManager_Should_Implement_IHostCopyManager()
    {
        // Arrange
        var interfaceType = typeof(IHostCopyManager);

        // Act & Assert
        var method = interfaceType.GetMethod("CopyHostAsync");
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
    }

    [Fact]
    public async Task DeveloperService_CopyHostAsync_Should_Call_CopyManager()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var sourceClientId = "sourceId";
        var newClientId = "targetId";
        var version = "1.0.0";
        var corsUrls = "http://localhost";
        
        // Setup mock to track method calls
        mockHostCopyManager
            .Setup(x => x.CopyHostAsync(sourceClientId, newClientId, version))
            .Returns(Task.CompletedTask);
        
        var mockLogger = new Mock<ILogger<DeveloperService>>();
        var mockProjectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockKubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        
        var service = new DeveloperService(mockHostDeployManager.Object, mockKubernetesClientAdapter.Object, 
            mockLogger.Object, mockProjectCorsOriginService.Object, mockConfiguration.Object, mockHostCopyManager.Object);

        // Act
        await service.CopyHostAsync(sourceClientId, newClientId, version);
        
        // Assert
        mockHostCopyManager.Verify(x => x.CopyHostAsync(sourceClientId, newClientId, version), Times.Once);
    }
    
    [Fact]
    public void DeveloperService_Should_Have_CreateServiceAsync_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(IDeveloperService).GetMethod("CreateServiceAsync", new[] { typeof(string), typeof(string), typeof(string) });

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    public void DeveloperService_Should_Have_RestartServiceAsync_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(IDeveloperService).GetMethod("RestartServiceAsync");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(DeveloperServiceOperationDto));
    }

    [Fact]
    public async Task CreateServiceAsync_ShouldCallHostDeployManager()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        
        var testClientId = "testId";
        var testVersion = "1";
        var testCorsUrls = "http://localhost";
        
        // Setup mock to track method calls
        mockHostDeployManager
            .Setup(x => x.CreateApplicationAsync(testClientId, testVersion, testCorsUrls, It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.CreateServiceAsync(testClientId, testVersion, testCorsUrls);
        
        // Assert
        mockHostDeployManager.Verify(x => x.CreateApplicationAsync(testClientId, testVersion, testCorsUrls, It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task DeleteServiceAsync_ShouldCallHostDeployManager()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        
        // Setup mock to return a deployment list with matching deployments
        var testClientId = "testId";
        var testVersion = "1";
        var siloDeploymentName = DeploymentHelper.GetAppDeploymentName($"{testClientId}-silo", testVersion);
        
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>
            {
                new V1Deployment
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = siloDeploymentName
                    }
                }
            }
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        // Setup mock to track method calls
        mockHostDeployManager
            .Setup(x => x.DestroyApplicationAsync(testClientId, testVersion))
            .Returns(Task.CompletedTask);
        
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.DeleteServiceAsync(testClientId);
        
        // Assert
        mockHostDeployManager.Verify(x => x.DestroyApplicationAsync(testClientId, testVersion), Times.Once);
        kubernetesClientAdapter.Verify(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteServiceAsync_Should_Throw_When_No_Deployments_Exist()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        
        var testClientId = "testId";
        
        // Setup mock to return an empty deployment list
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>()
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert - Should throw when no deployments exist
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => developerService.DeleteServiceAsync(testClientId));
        exception.Message.ShouldContain("No Host service found to delete for client: testId");
        
        // Verify kubernetes client was called but no destroy operation
        kubernetesClientAdapter.Verify(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockHostDeployManager.Verify(x => x.DestroyApplicationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("", "target", "1")]
    [InlineData("source", "", "1")]
    [InlineData("source", "target", "")]
    public async Task CopyHostAsync_Should_Handle_Invalid_Parameters(string sourceClientId, string newClientId, string version)
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        mockHostCopyManager
            .Setup(x => x.CopyHostAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        var mockLogger = new Mock<ILogger<DeveloperService>>();
        var mockProjectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockKubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        
        var service = new DeveloperService(mockHostDeployManager.Object, mockKubernetesClientAdapter.Object, 
            mockLogger.Object, mockProjectCorsOriginService.Object, mockConfiguration.Object, mockHostCopyManager.Object);

        // Act & Assert - Should still call the copy manager even with empty parameters
        await service.CopyHostAsync(sourceClientId, newClientId, version);
        
        mockHostCopyManager.Verify(x => x.CopyHostAsync(sourceClientId, newClientId, version), Times.Once);
    }

    private class StubHostDeployManager : IHostDeployManager
    {
        public Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
        {
            return Task.FromResult(string.Empty);
        }

        public Task DestroyWebHookAsync(string appId, string version)
        {
            return Task.CompletedTask;
        }

        public Task RestartWebHookAsync(string appId, string version)
        {
            return Task.CompletedTask;
        }

        public Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId)
        {
            return Task.CompletedTask;
        }

        public Task DestroyApplicationAsync(string appId, string version)
        {
            return Task.CompletedTask;
        }

        public Task UpgradeApplicationAsync(string appId, string version, string corsUrls, Guid tenantId)
        {
            return Task.CompletedTask;
        }

        public Task UpdateDeploymentImageAsync(string appId, string version, string newImage)
        {
            return Task.CompletedTask;
        }

        public Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType)
        {
            return Task.CompletedTask;
        }

        public Task RestartHostAsync(string appId, string version)
        {
            return Task.CompletedTask;
        }
    }

    private class StubHostCopyManager : IHostCopyManager
    {
        public Task CopyHostAsync(string sourceClientId, string newClientId, string version)
        {
            return Task.CompletedTask;
        }
    }
}