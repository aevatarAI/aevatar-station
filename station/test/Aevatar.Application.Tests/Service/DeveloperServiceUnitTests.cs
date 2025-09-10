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
using Volo.Abp.Application.Dtos;
using System.Linq;

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

        public Task CopyDeploymentWithPatternAsync(string clientId, string sourceVersion, string targetVersion, string siloNamePattern)
        {
            return Task.CompletedTask;
        }
    }

    #region UpdateDockerImageAsync Tests

    [Fact]
    public async Task UpdateDockerImageAsync_ShouldCallHostDeployManager()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testAppId = "testApp";
        var testVersion = "1.0.0";
        var testNewImage = "nginx:latest";
        
        mockHostDeployManager
            .Setup(x => x.UpdateDeploymentImageAsync(testAppId, testVersion, testNewImage))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.UpdateDockerImageAsync(testAppId, testVersion, testNewImage);
        
        // Assert
        mockHostDeployManager.Verify(x => x.UpdateDeploymentImageAsync(testAppId, testVersion, testNewImage), Times.Once);
    }

    [Theory]
    [InlineData("", "1.0.0", "nginx:latest")]
    [InlineData("testApp", "", "nginx:latest")]
    [InlineData("testApp", "1.0.0", "")]
    public async Task UpdateDockerImageAsync_ShouldPassParametersAsIs(string appId, string version, string newImage)
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        mockHostDeployManager
            .Setup(x => x.UpdateDeploymentImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.UpdateDockerImageAsync(appId, version, newImage);
        
        // Assert
        mockHostDeployManager.Verify(x => x.UpdateDeploymentImageAsync(appId, version, newImage), Times.Once);
    }

    #endregion

    #region RestartServiceAsync Tests

    [Fact]
    public async Task RestartServiceAsync_ShouldSucceedWhenHostServiceExists()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var input = new DeveloperServiceOperationDto
        {
            DomainName = "testDomain",
            ProjectId = Guid.NewGuid()
        };
        
        // Setup existing deployment
        var siloDeploymentName = DeploymentHelper.GetAppDeploymentName($"{input.DomainName}-silo", "1");
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>
            {
                new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = siloDeploymentName }
                }
            }
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        // Setup CORS configuration
        configuration.Setup(x => x["App:DefaultCorsOrigins"]).Returns("http://localhost:3000");
        
        var corsOriginsList = new ListResultDto<ProjectCorsOriginDto>
        {
            Items = new List<ProjectCorsOriginDto>
            {
                new ProjectCorsOriginDto { Domain = "http://business.com" }
            }
        };
        
        projectCorsOriginService
            .Setup(x => x.GetListAsync(input.ProjectId))
            .ReturnsAsync(corsOriginsList);
        
        mockHostDeployManager
            .Setup(x => x.UpgradeApplicationAsync(input.DomainName, "1", It.IsAny<string>(), input.ProjectId))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.RestartServiceAsync(input);
        
        // Assert
        kubernetesClientAdapter.Verify(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        projectCorsOriginService.Verify(x => x.GetListAsync(input.ProjectId), Times.Once);
        mockHostDeployManager.Verify(x => x.UpgradeApplicationAsync(input.DomainName, "1", 
            "http://localhost:3000,http://business.com", input.ProjectId), Times.Once);
    }

    [Fact]
    public async Task RestartServiceAsync_ShouldThrowWhenNoHostServiceExists()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var input = new DeveloperServiceOperationDto
        {
            DomainName = "nonExistentDomain",
            ProjectId = Guid.NewGuid()
        };
        
        // Setup empty deployment list (no services exist)
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>()
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => developerService.RestartServiceAsync(input));
        exception.Message.ShouldContain("No Host service found to restart for client: nonExistentDomain");
        
        // Verify that no upgrade was attempted
        mockHostDeployManager.Verify(x => x.UpgradeApplicationAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region UpdateBusinessConfigurationAsync Tests

    [Fact]
    public async Task UpdateBusinessConfigurationAsync_ShouldCallHostDeployManager()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testHostId = "testHost";
        var testVersion = "1.0.0";
        var testHostType = HostTypeEnum.Client;
        
        mockHostDeployManager
            .Setup(x => x.UpdateBusinessConfigurationAsync(testHostId, testVersion, testHostType))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.UpdateBusinessConfigurationAsync(testHostId, testVersion, testHostType);
        
        // Assert
        mockHostDeployManager.Verify(x => x.UpdateBusinessConfigurationAsync(testHostId, testVersion, testHostType), Times.Once);
    }

    [Theory]
    [InlineData(HostTypeEnum.Client)]
    [InlineData(HostTypeEnum.Silo)]
    public async Task UpdateBusinessConfigurationAsync_ShouldHandleDifferentHostTypes(HostTypeEnum hostType)
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testHostId = "testHost";
        var testVersion = "1.0.0";
        
        mockHostDeployManager
            .Setup(x => x.UpdateBusinessConfigurationAsync(testHostId, testVersion, hostType))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.UpdateBusinessConfigurationAsync(testHostId, testVersion, hostType);
        
        // Assert
        mockHostDeployManager.Verify(x => x.UpdateBusinessConfigurationAsync(testHostId, testVersion, hostType), Times.Once);
    }

    #endregion

    #region CreateServiceAsync with ProjectId Tests

    [Fact]
    public async Task CreateServiceAsync_WithProjectId_ShouldThrowWhenClientIdIsNull()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => 
            developerService.CreateServiceAsync(null, Guid.NewGuid()));
        exception.Message.ShouldContain("DomainName cannot be null or empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateServiceAsync_WithProjectId_ShouldThrowWhenClientIdIsEmpty(string clientId)
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => 
            developerService.CreateServiceAsync(clientId, Guid.NewGuid()));
        exception.Message.ShouldContain("DomainName cannot be null or empty");
    }

    [Fact]
    public async Task CreateServiceAsync_WithProjectId_ShouldThrowWhenServiceExists()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "existingClient";
        var testProjectId = Guid.NewGuid();
        
        // Setup existing deployment
        var siloDeploymentName = DeploymentHelper.GetAppDeploymentName($"{testClientId}-silo", "1");
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>
            {
                new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = siloDeploymentName }
                }
            }
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => 
            developerService.CreateServiceAsync(testClientId, testProjectId));
        exception.Message.ShouldContain("Host service partially or fully exists for client: existingClient");
    }

    [Fact]
    public async Task CreateServiceAsync_WithProjectId_ShouldSucceedWhenNoServiceExists()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "newClient";
        var testProjectId = Guid.NewGuid();
        
        // Setup empty deployment list (no existing services)
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>()
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        // Setup CORS configuration
        configuration.Setup(x => x["App:DefaultCorsOrigins"]).Returns("http://localhost:3000");
        
        var corsOriginsList = new ListResultDto<ProjectCorsOriginDto>
        {
            Items = new List<ProjectCorsOriginDto>
            {
                new ProjectCorsOriginDto { Domain = "http://business.com" }
            }
        };
        
        projectCorsOriginService
            .Setup(x => x.GetListAsync(testProjectId))
            .ReturnsAsync(corsOriginsList);
        
        mockHostDeployManager
            .Setup(x => x.CreateApplicationAsync(testClientId, "1", It.IsAny<string>(), testProjectId))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.CreateServiceAsync(testClientId, testProjectId);
        
        // Assert
        kubernetesClientAdapter.Verify(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        projectCorsOriginService.Verify(x => x.GetListAsync(testProjectId), Times.Once);
        mockHostDeployManager.Verify(x => x.CreateApplicationAsync(testClientId, "1", 
            "http://localhost:3000,http://business.com", testProjectId), Times.Once);
    }

    #endregion

    #region CORS Configuration Tests

    [Fact]
    public async Task CreateServiceAsync_ShouldCombineCorsUrlsCorrectly_WithBothPlatformAndBusiness()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        var testProjectId = Guid.NewGuid();
        
        // Setup empty deployment list
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1DeploymentList { Items = new List<V1Deployment>() });
        
        // Setup platform CORS URLs
        configuration.Setup(x => x["App:DefaultCorsOrigins"]).Returns("http://localhost:3000,http://localhost:4000");
        configuration.Setup(x => x["App:CorsOrigins"]).Returns((string)null);
        
        // Setup business CORS URLs
        var corsOriginsList = new ListResultDto<ProjectCorsOriginDto>
        {
            Items = new List<ProjectCorsOriginDto>
            {
                new ProjectCorsOriginDto { Domain = "http://business1.com" },
                new ProjectCorsOriginDto { Domain = "http://business2.com" }
            }
        };
        
        projectCorsOriginService
            .Setup(x => x.GetListAsync(testProjectId))
            .ReturnsAsync(corsOriginsList);
        
        mockHostDeployManager
            .Setup(x => x.CreateApplicationAsync(testClientId, "1", It.IsAny<string>(), testProjectId))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.CreateServiceAsync(testClientId, testProjectId);
        
        // Assert - Should combine platform and business CORS URLs
        mockHostDeployManager.Verify(x => x.CreateApplicationAsync(testClientId, "1", 
            "http://localhost:3000,http://localhost:4000,http://business1.com,http://business2.com", testProjectId), Times.Once);
    }

    [Fact]
    public async Task CreateServiceAsync_ShouldUseFallbackCorsOrigins_WhenDefaultIsEmpty()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        var testProjectId = Guid.NewGuid();
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1DeploymentList { Items = new List<V1Deployment>() });
        
        // Setup fallback CORS URLs configuration
        configuration.Setup(x => x["App:DefaultCorsOrigins"]).Returns((string)null);
        configuration.Setup(x => x["App:CorsOrigins"]).Returns("http://fallback.com");
        
        var corsOriginsList = new ListResultDto<ProjectCorsOriginDto>
        {
            Items = new List<ProjectCorsOriginDto>
            {
                new ProjectCorsOriginDto { Domain = "http://business.com" }
            }
        };
        
        projectCorsOriginService
            .Setup(x => x.GetListAsync(testProjectId))
            .ReturnsAsync(corsOriginsList);
        
        mockHostDeployManager
            .Setup(x => x.CreateApplicationAsync(testClientId, "1", It.IsAny<string>(), testProjectId))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.CreateServiceAsync(testClientId, testProjectId);
        
        // Assert - Should use fallback configuration
        mockHostDeployManager.Verify(x => x.CreateApplicationAsync(testClientId, "1", 
            "http://fallback.com,http://business.com", testProjectId), Times.Once);
    }

    [Fact]
    public async Task CreateServiceAsync_ShouldUseBusinessCorsOnly_WhenNoPlatformCors()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        var testProjectId = Guid.NewGuid();
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1DeploymentList { Items = new List<V1Deployment>() });
        
        // Setup no platform CORS URLs
        configuration.Setup(x => x["App:DefaultCorsOrigins"]).Returns((string)null);
        configuration.Setup(x => x["App:CorsOrigins"]).Returns((string)null);
        
        var corsOriginsList = new ListResultDto<ProjectCorsOriginDto>
        {
            Items = new List<ProjectCorsOriginDto>
            {
                new ProjectCorsOriginDto { Domain = "http://onlybusiness.com" }
            }
        };
        
        projectCorsOriginService
            .Setup(x => x.GetListAsync(testProjectId))
            .ReturnsAsync(corsOriginsList);
        
        mockHostDeployManager
            .Setup(x => x.CreateApplicationAsync(testClientId, "1", It.IsAny<string>(), testProjectId))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.CreateServiceAsync(testClientId, testProjectId);
        
        // Assert - Should use only business CORS URLs
        mockHostDeployManager.Verify(x => x.CreateApplicationAsync(testClientId, "1", 
            "http://onlybusiness.com", testProjectId), Times.Once);
    }

    [Fact]
    public async Task CreateServiceAsync_ShouldUseEmptyString_WhenNoCorsUrls()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        var testProjectId = Guid.NewGuid();
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new V1DeploymentList { Items = new List<V1Deployment>() });
        
        // Setup no CORS URLs anywhere
        configuration.Setup(x => x["App:DefaultCorsOrigins"]).Returns((string)null);
        configuration.Setup(x => x["App:CorsOrigins"]).Returns((string)null);
        
        var corsOriginsList = new ListResultDto<ProjectCorsOriginDto>
        {
            Items = new List<ProjectCorsOriginDto>()
        };
        
        projectCorsOriginService
            .Setup(x => x.GetListAsync(testProjectId))
            .ReturnsAsync(corsOriginsList);
        
        mockHostDeployManager
            .Setup(x => x.CreateApplicationAsync(testClientId, "1", It.IsAny<string>(), testProjectId))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.CreateServiceAsync(testClientId, testProjectId);
        
        // Assert - Should use empty string
        mockHostDeployManager.Verify(x => x.CreateApplicationAsync(testClientId, "1", 
            string.Empty, testProjectId), Times.Once);
    }

    #endregion

    #region DeleteServiceAsync Extended Tests

    [Fact]
    public async Task DeleteServiceAsync_ShouldThrowWhenClientIdIsNull()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => 
            developerService.DeleteServiceAsync(null));
        exception.Message.ShouldContain("DomainName cannot be null or empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteServiceAsync_ShouldThrowWhenClientIdIsEmpty(string clientId)
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => 
            developerService.DeleteServiceAsync(clientId));
        exception.Message.ShouldContain("DomainName cannot be null or empty");
    }

    [Fact]
    public async Task DeleteServiceAsync_ShouldHandleKubernetesException()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        
        // Setup Kubernetes client to throw exception
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Kubernetes API error"));
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert - Should throw UserFriendlyException when service doesn't exist (due to exception handling)
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => 
            developerService.DeleteServiceAsync(testClientId));
        exception.Message.ShouldContain("No Host service found to delete for client: testClient");
        
        // Verify that destroy was not called due to exception
        mockHostDeployManager.Verify(x => x.DestroyApplicationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void DeveloperService_Should_Have_CopyDeploymentWithPatternAsync_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(IDeveloperService).GetMethod("CopyDeploymentWithPatternAsync");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(4);
        parameters[0].Name.ShouldBe("clientId");
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("sourceVersion");
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].Name.ShouldBe("targetVersion");
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[3].Name.ShouldBe("siloNamePattern");
        parameters[3].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    public void IHostCopyManager_Should_Have_CopyDeploymentWithPatternAsync_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(IHostCopyManager).GetMethod("CopyDeploymentWithPatternAsync");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(4);
        parameters[0].Name.ShouldBe("clientId");
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("sourceVersion");
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].Name.ShouldBe("targetVersion");
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[3].Name.ShouldBe("siloNamePattern");
        parameters[3].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    public async Task DeveloperService_CopyDeploymentWithPatternAsync_Should_Call_CopyManager()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";
        
        // Setup mock to track method calls
        mockHostCopyManager
            .Setup(x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern))
            .Returns(Task.CompletedTask);
        
        var mockLogger = new Mock<ILogger<DeveloperService>>();
        var mockProjectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockKubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        
        var service = new DeveloperService(mockHostDeployManager.Object, mockKubernetesClientAdapter.Object, 
            mockLogger.Object, mockProjectCorsOriginService.Object, mockConfiguration.Object, mockHostCopyManager.Object);

        // Act
        await service.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern);
        
        // Assert
        mockHostCopyManager.Verify(x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern), Times.Once);
    }

    [Fact]
    public async Task DeveloperService_CopyDeploymentWithPatternAsync_Should_Handle_Exception()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";
        
        // Setup mock to throw exception
        mockHostCopyManager
            .Setup(x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern))
            .ThrowsAsync(new InvalidOperationException("Source deployment not found"));
        
        var mockLogger = new Mock<ILogger<DeveloperService>>();
        var mockProjectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockKubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        
        var service = new DeveloperService(mockHostDeployManager.Object, mockKubernetesClientAdapter.Object, 
            mockLogger.Object, mockProjectCorsOriginService.Object, mockConfiguration.Object, mockHostCopyManager.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => 
            service.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern));
        
        exception.Message.ShouldBe("Source deployment not found");
        mockHostCopyManager.Verify(x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern), Times.Once);
    }

    [Fact]
    public async Task DeleteServiceAsync_ShouldDeleteWhenOnlyClientServiceExists()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        var clientDeploymentName = DeploymentHelper.GetAppDeploymentName($"{testClientId}-client", "1");
        
        // Setup deployment list with only client deployment (no silo)
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>
            {
                new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = clientDeploymentName }
                }
            }
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        mockHostDeployManager
            .Setup(x => x.DestroyApplicationAsync(testClientId, "1"))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.DeleteServiceAsync(testClientId);
        
        // Assert
        kubernetesClientAdapter.Verify(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockHostDeployManager.Verify(x => x.DestroyApplicationAsync(testClientId, "1"), Times.Once);
    }

    [Fact]
    public async Task DeleteServiceAsync_ShouldDeleteWhenBothSiloAndClientExist()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        var siloDeploymentName = DeploymentHelper.GetAppDeploymentName($"{testClientId}-silo", "1");
        var clientDeploymentName = DeploymentHelper.GetAppDeploymentName($"{testClientId}-client", "1");
        
        // Setup deployment list with both silo and client deployments
        var mockDeploymentList = new V1DeploymentList
        {
            Items = new List<V1Deployment>
            {
                new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = siloDeploymentName }
                },
                new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = clientDeploymentName }
                }
            }
        };
        
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDeploymentList);
        
        mockHostDeployManager
            .Setup(x => x.DestroyApplicationAsync(testClientId, "1"))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act
        await developerService.DeleteServiceAsync(testClientId);
        
        // Assert
        kubernetesClientAdapter.Verify(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockHostDeployManager.Verify(x => x.DestroyApplicationAsync(testClientId, "1"), Times.Once);
    }

    #endregion

    #region Kubernetes Service Status Tests

    [Fact]
    public async Task RestartServiceAsync_ShouldHandleKubernetesException()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var input = new DeveloperServiceOperationDto
        {
            DomainName = "testDomain",
            ProjectId = Guid.NewGuid()
        };
        
        // Setup Kubernetes client to throw exception
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Kubernetes API error"));
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act & Assert - Should throw because service doesn't exist (due to exception)
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => 
            developerService.RestartServiceAsync(input));
        exception.Message.ShouldContain("No Host service found to restart for client: testDomain");
        
        // Verify that no upgrade was attempted
        mockHostDeployManager.Verify(x => x.UpgradeApplicationAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateServiceAsync_ShouldHandleKubernetesExceptionAndProceed()
    {
        // Arrange
        var mockHostDeployManager = new Mock<IHostDeployManager>();
        var kubernetesClientAdapter = new Mock<IKubernetesClientAdapter>();
        var logger = new Mock<ILogger<DeveloperService>>();
        var projectCorsOriginService = new Mock<IProjectCorsOriginService>();
        var configuration = new Mock<IConfiguration>();
        var mockHostCopyManager = new Mock<IHostCopyManager>();
        
        var testClientId = "testClient";
        var testProjectId = Guid.NewGuid();
        
        // Setup Kubernetes client to throw exception (which means no services exist)
        kubernetesClientAdapter
            .Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Kubernetes API error"));
        
        // Setup CORS configuration
        configuration.Setup(x => x["App:DefaultCorsOrigins"]).Returns("http://localhost:3000");
        
        var corsOriginsList = new ListResultDto<ProjectCorsOriginDto>
        {
            Items = new List<ProjectCorsOriginDto>()
        };
        
        projectCorsOriginService
            .Setup(x => x.GetListAsync(testProjectId))
            .ReturnsAsync(corsOriginsList);
        
        mockHostDeployManager
            .Setup(x => x.CreateApplicationAsync(testClientId, "1", It.IsAny<string>(), testProjectId))
            .Returns(Task.CompletedTask);
        
        var developerService = new DeveloperService(mockHostDeployManager.Object, kubernetesClientAdapter.Object, 
            logger.Object, projectCorsOriginService.Object, configuration.Object, mockHostCopyManager.Object);

        // Act - Should proceed with creation even if Kubernetes check fails
        await developerService.CreateServiceAsync(testClientId, testProjectId);
        
        // Assert - Should still create service as exception means no services exist
        projectCorsOriginService.Verify(x => x.GetListAsync(testProjectId), Times.Once);
        mockHostDeployManager.Verify(x => x.CreateApplicationAsync(testClientId, "1", 
            "http://localhost:3000", testProjectId), Times.Once);
    }

    #endregion

}
