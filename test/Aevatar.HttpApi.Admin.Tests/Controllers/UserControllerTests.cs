using System.Security.Claims;
using Aevatar.Admin.Controllers;
using Aevatar.Kubernetes.Enum;
using Aevatar.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aevatar.HttpApi.Admin.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserAppService> _userAppServiceMock;
    private readonly Mock<IDeveloperService> _developerServiceMock;
    private readonly Mock<ILogger<UserController>> _loggerMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userAppServiceMock = new Mock<IUserAppService>();
        _developerServiceMock = new Mock<IDeveloperService>();
        _loggerMock = new Mock<ILogger<UserController>>();
        
        _controller = new UserController(_userAppServiceMock.Object, _developerServiceMock.Object, _loggerMock.Object);
        
        // Setup ClaimsPrincipal for authorization tests
        var claims = new[]
        {
            new Claim("client_id", "test-client")
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task RegisterClientAuthentication_ShouldCallServices()
    {
        // Arrange
        var clientId = "test-client";
        var clientSecret = "test-secret";
        var corsUrls = "http://localhost:3000";

        // Act
        await _controller.RegisterClientAuthentication(clientId, clientSecret, corsUrls);

        // Assert
        _userAppServiceMock.Verify(x => x.RegisterClientAuthentication(clientId, clientSecret), Times.Once);
        _developerServiceMock.Verify(x => x.CreateHostAsync(clientId, "1", corsUrls), Times.Once);
    }

    [Fact]
    public async Task GrantClientPermissions_ShouldCallService()
    {
        // Arrange
        var clientId = "test-client";

        // Act
        await _controller.GrantClientPermissionsAsync(clientId);

        // Assert
        _userAppServiceMock.Verify(x => x.GrantClientPermissionsAsync(clientId), Times.Once);
    }

    [Fact]
    public async Task CreateHost_ShouldCallService()
    {
        // Arrange
        var clientId = "test-client";
        var corsUrls = "http://localhost:3000";

        // Act
        await _controller.CreateHost(clientId, corsUrls);

        // Assert
        _developerServiceMock.Verify(x => x.CreateHostAsync(clientId, "1", corsUrls), Times.Once);
    }

    [Fact]
    public async Task DestroyHost_ShouldCallService()
    {
        // Arrange
        var clientId = "test-client";

        // Act
        await _controller.DestroyHostAsync(clientId);

        // Assert
        _developerServiceMock.Verify(x => x.DestroyHostAsync(clientId, "1"), Times.Once);
    }

    // [Fact] TODO need fix
    // public async Task UpdateDockerImage_WithValidClient_ShouldCallService()
    // {
    //     // Arrange
    //     var hostType = HostTypeEnum.Client;
    //     var imageName = "test-image:latest";
    //
    //     // Act
    //     await _controller.UpdateDockerImageAsync(hostType, imageName);
    //
    //     // Assert
    //     _developerServiceMock.Verify(x => x.UpdateDockerImageAsync("test-client-" + hostType, "1", imageName), Times.Once);
    // }

    [Fact]
    public async Task UpdateDockerImageByAdmin_ShouldCallService()
    {
        // Arrange
        var hostId = "test-host";
        var hostType = HostTypeEnum.Client;
        var imageName = "test-image:latest";

        // Act
        await _controller.UpdateDockerImageByAdminAsync(hostId, hostType, imageName);

        // Assert
        _developerServiceMock.Verify(x => x.UpdateDockerImageAsync(hostId + "-" + hostType, "1", imageName), Times.Once);
    }
}
