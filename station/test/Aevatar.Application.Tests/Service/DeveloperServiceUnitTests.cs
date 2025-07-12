// ABOUTME: This file contains isolated unit tests for DeveloperService copy host functionality
// ABOUTME: Tests validate basic method signature and interface implementation

using System;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Service;
using Aevatar.WebHook.Deploy;
using Shouldly;
using Xunit;

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
        parameters.Length.ShouldBe(4);
        parameters[0].Name.ShouldBe("sourceClientId");
        parameters[1].Name.ShouldBe("newClientId");
        parameters[2].Name.ShouldBe("version");
        parameters[3].Name.ShouldBe("corsUrls");
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
    public void IHostDeployManager_Should_Have_CopyHostAsync_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(IHostDeployManager).GetMethod("CopyHostAsync");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(4);
        parameters[0].Name.ShouldBe("sourceClientId");
        parameters[1].Name.ShouldBe("newClientId");
        parameters[2].Name.ShouldBe("version");
        parameters[3].Name.ShouldBe("corsUrls");
    }

    [Fact]
    public void DefaultHostDeployManager_Should_Implement_CopyHostAsync()
    {
        // Arrange
        var serviceType = typeof(DefaultHostDeployManager);
        var interfaceType = typeof(IHostDeployManager);

        // Act & Assert
        interfaceType.IsAssignableFrom(serviceType).ShouldBeTrue();
        
        var method = serviceType.GetMethod("CopyHostAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeveloperService_CopyHostAsync_Should_Not_Throw_With_Valid_Parameters()
    {
        // Arrange
        var mockHostDeployManager = new StubHostDeployManager();
        var service = new DeveloperService(mockHostDeployManager);

        // Act & Assert
        await Should.NotThrowAsync(() => service.CopyHostAsync("source", "target", "1", "cors"));
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

        public Task<string> CreateHostAsync(string appId, string version, string corsUrls)
        {
            return Task.FromResult(string.Empty);
        }

        public Task DestroyHostAsync(string appId, string version)
        {
            return Task.CompletedTask;
        }

        public Task RestartHostAsync(string appId, string version)
        {
            return Task.CompletedTask;
        }

        public Task UpdateDockerImageAsync(string appId, string version, string newImage)
        {
            return Task.CompletedTask;
        }

        public Task CopyHostAsync(string sourceClientId, string newClientId, string version, string corsUrls)
        {
            return Task.CompletedTask;
        }
    }
}