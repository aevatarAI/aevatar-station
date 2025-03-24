// using System.Security.Claims;
// using Aevatar.Handler;
// using Aevatar.PermissionManagement;
// using Microsoft.AspNetCore.Authentication;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Authorization.Policy;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Moq;
// using OpenIddict.Abstractions;
// using Xunit;
//
// namespace Aevatar.HttpApi.Tests.Handler;
//
// public class AevatarAuthorizationMiddlewareResultHandlerTests : IDisposable
// {
//     private readonly AevatarAuthorizationMiddlewareResultHandler _handler;
//     private readonly Mock<HttpContext> _httpContextMock;
//     private readonly AuthorizationPolicy _policy;
//     private readonly PolicyAuthorizationResult _policyAuthorizationResult;
//     private readonly Mock<ClaimsIdentity> _identityMock;
//     private readonly Mock<ClaimsPrincipal> _userMock;
//     private readonly ServiceProvider _serviceProvider;
//     private readonly Mock<IAuthenticationService> _authServiceMock;
//
//     public AevatarAuthorizationMiddlewareResultHandlerTests()
//     {
//         _handler = new AevatarAuthorizationMiddlewareResultHandler();
//         _httpContextMock = new Mock<HttpContext>();
//         _policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
//         _policyAuthorizationResult = PolicyAuthorizationResult.Success();
//
//         // Setup authentication service mock
//         _authServiceMock = new Mock<IAuthenticationService>();
//         _authServiceMock
//             .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
//             .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(
//                 new ClaimsPrincipal(new ClaimsIdentity()), "Test")));
//
//         // Setup service provider
//         var services = new ServiceCollection();
//         services.AddLogging();
//         services.AddOptions();
//         services.AddAuthentication(o =>
//         {
//             o.DefaultScheme = "Test";
//             o.DefaultAuthenticateScheme = "Test";
//         });
//         services.AddAuthorization();
//         services.AddSingleton(_authServiceMock.Object);
//         services.AddScoped<UserContext>();
//
//         _serviceProvider = services.BuildServiceProvider();
//         _httpContextMock.Setup(x => x.RequestServices).Returns(_serviceProvider);
//
//         // Setup identity and user mocks
//         _identityMock = new Mock<ClaimsIdentity>();
//         _userMock = new Mock<ClaimsPrincipal>();
//         _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
//     }
//
//     public void Dispose()
//     {
//         _serviceProvider?.Dispose();
//     }
//
//     [Fact]
//     public async Task HandleAsync_WithUnauthenticatedUser_ShouldNotSetRequestContext()
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var userContext = scope.ServiceProvider.GetRequiredService<UserContext>();
//         
//         // Arrange
//         var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);
//         _identityMock.Setup(x => x.IsAuthenticated).Returns(false);
//         _userMock.Setup(x => x.Identity).Returns(_identityMock.Object);
//         _httpContextMock.Setup(x => x.RequestServices).Returns(scope.ServiceProvider);
//
//         // Act
//         await _handler.HandleAsync(requestDelegate, _httpContextMock.Object, _policy, _policyAuthorizationResult);
//
//         // Assert
//         Assert.Equal(Guid.Empty, userContext.UserId);
//         Assert.Empty(userContext.Roles);
//         Assert.Null(userContext.ClientId);
//     }
//
//     [Fact]
//     public async Task HandleAsync_WithAuthenticatedUser_ShouldSetRequestContext()
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var userContext = scope.ServiceProvider.GetRequiredService<UserContext>();
//         
//         // Arrange
//         var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);
//         var userId = Guid.NewGuid().ToString();
//         var roles = new[] { "admin", "user" };
//         var clientId = "test-client";
//
//         var claims = new List<Claim>
//         {
//             new(OpenIddictConstants.Claims.Subject, userId),
//             new(OpenIddictConstants.Claims.ClientId, clientId)
//         };
//         foreach (var role in roles)
//         {
//             claims.Add(new Claim(OpenIddictConstants.Claims.Role, role));
//         }
//
//         _identityMock.Setup(x => x.IsAuthenticated).Returns(true);
//         _userMock.Setup(x => x.Identity).Returns(_identityMock.Object);
//         _userMock.Setup(x => x.FindFirst(OpenIddictConstants.Claims.Subject))
//             .Returns(claims.First(c => c.Type == OpenIddictConstants.Claims.Subject));
//         _userMock.Setup(x => x.FindFirst(OpenIddictConstants.Claims.ClientId))
//             .Returns(claims.First(c => c.Type == OpenIddictConstants.Claims.ClientId));
//         _userMock.Setup(x => x.FindAll(OpenIddictConstants.Claims.Role))
//             .Returns(claims.Where(c => c.Type == OpenIddictConstants.Claims.Role));
//         _httpContextMock.Setup(x => x.RequestServices).Returns(scope.ServiceProvider);
//
//         // Act
//         await _handler.HandleAsync(requestDelegate, _httpContextMock.Object, _policy, _policyAuthorizationResult);
//
//         // Assert
//         // Assert.Equal(Guid.Parse(userId), userContext.UserId);
//         Assert.Equal(roles, userContext.Roles);
//         Assert.Equal(clientId, userContext.ClientId);
//     }
//
//     [Fact]
//     public async Task HandleAsync_WithFailureResult_ShouldSetUnauthorizedStatusCode()
//     {
//         // Arrange
//         var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);
//         var failureResult = PolicyAuthorizationResult.Forbid();
//         var response = new Mock<HttpResponse>();
//         _httpContextMock.Setup(x => x.Response).Returns(response.Object);
//         _identityMock.Setup(x => x.IsAuthenticated).Returns(false);
//         _userMock.Setup(x => x.Identity).Returns(_identityMock.Object);
//
//         // Act
//         await _handler.HandleAsync(requestDelegate, _httpContextMock.Object, _policy, failureResult);
//
//         // Assert
//         response.VerifySet(x => x.StatusCode = StatusCodes.Status403Forbidden, Times.Once);
//     }
// }
