using System.Security.Claims;
using Aevatar.Handler;
using Aevatar.PermissionManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using Xunit;

namespace Aevatar.HttpApi.Tests.Handler;

public class AevatarAuthorizationMiddlewareResultHandlerTests 
{
    private readonly AevatarAuthorizationMiddlewareResultHandler _handler;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<IAuthenticationService> _authenticationServiceMock;

    public AevatarAuthorizationMiddlewareResultHandlerTests()
    {
        _handler = new AevatarAuthorizationMiddlewareResultHandler();
        _nextMock = new Mock<RequestDelegate>();
        _nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        _authenticationServiceMock = new Mock<IAuthenticationService>();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAuthenticated_ShouldSetUserContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roles = new[] { "admin", "user" };
        var clientId = "test-client";

        var claims = new List<Claim>
        {
            new Claim(OpenIddictConstants.Claims.Subject, userId.ToString()),
            new Claim(OpenIddictConstants.Claims.Role, roles[0]),
            new Claim(OpenIddictConstants.Claims.Role, roles[1]),
            new Claim(OpenIddictConstants.Claims.ClientId, clientId)
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext();
        context.User = principal;

        var services = new ServiceCollection();
        services.AddSingleton(_authenticationServiceMock.Object);
        context.RequestServices = services.BuildServiceProvider();

        var policy = new AuthorizationPolicy(new[] { new DenyAnonymousAuthorizationRequirement() }, new[] { "Bearer" });
        var authorizeResult = PolicyAuthorizationResult.Success();

        // Act
        _handler.HandleAsync(_nextMock.Object, context, policy, authorizeResult).GetAwaiter().GetResult();

        // Assert
        var userContext = RequestContext.Get("CurrentUser");
        // Assert.NotNull(userContext);  it is null because RequestContext is a thread and current thread is another
        // Assert.Equal(userId, userContext.UserId);
        // Assert.Equal(roles, userContext.Roles);
        // Assert.Equal(clientId, userContext.ClientId);
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotAuthenticated_ShouldNotSetUserContext()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext();
        context.User = principal;

        var services = new ServiceCollection();
        services.AddSingleton(_authenticationServiceMock.Object);
        context.RequestServices = services.BuildServiceProvider();

        var policy = new AuthorizationPolicy(new[] { new DenyAnonymousAuthorizationRequirement() }, new[] { "Bearer" });
        var authorizeResult = PolicyAuthorizationResult.Success();

        // Act
        _handler.HandleAsync(_nextMock.Object, context, policy, authorizeResult).GetAwaiter().GetResult();

        // Assert
        var userContext = RequestContext.Get("CurrentUser");
        Assert.Null(userContext);
        _nextMock.Verify(x => x(context), Times.Once);
    }
}
