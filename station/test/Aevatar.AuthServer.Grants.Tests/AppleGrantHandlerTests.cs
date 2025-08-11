using System.Collections.Specialized;
using System.Security.Claims;
using Aevatar.AuthServer.Grants.Options;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Xunit;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using MicrosoftIdentityUser = Microsoft.AspNetCore.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace Aevatar.AuthServer.Grants;

public abstract class AppleGrantHandlerTests<TStartupModule> : AevatarAuthServerGrantsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IdentityRoleManager _identityRoleManager;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly ILogger<AppleGrantHandler> _logger;

    protected AppleGrantHandlerTests()
    {
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _serviceProvider = GetRequiredService<IServiceProvider>();
        _identityRoleManager = GetRequiredService<IdentityRoleManager>();
        _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _logger = GetRequiredService<ILogger<AppleGrantHandler>>();
    }

    [Fact]
    public async Task Handle_Success_WithIdToken_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "apple.user.123"),
            new Claim(ClaimTypes.Email, "test@apple.com"),
            new Claim(ClaimTypes.GivenName, "John"),
            new Claim(ClaimTypes.Surname, "Doe")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((true, principal));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token.here");
        collection.Add("source", "ios");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        var basicRole = await _identityRoleManager.FindByNameAsync(Permissions.AevatarPermissions.BasicUser);
        var user = await _identityUserManager.FindByEmailAsync("test@apple.com");
        user.ShouldNotBeNull();
        user.Roles.ShouldContain(o => o.RoleId == basicRole.Id);
        
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.APPLE, "apple.user.123");
        loginUser.Id.ShouldBe(user.Id);
        
        var claimsPrincipal = context.HttpContext.User;
        claimsPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.ShouldBe(user.Id.ToString());
    }

    [Fact]
    public async Task Handle_Success_WithCode_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "apple.user.456"),
            new Claim(ClaimTypes.Email, "test2@apple.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        
        appleProvider.Setup(o => o.ExchangeCodeForTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync("exchanged.token.here");
        appleProvider.Setup(o => o.ValidateAppleTokenAsync("exchanged.token.here", It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((true, principal));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("code", "apple.auth.code");
        collection.Add("source", "web");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        var user = await _identityUserManager.FindByEmailAsync("test2@apple.com");
        user.ShouldNotBeNull();
        
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.APPLE, "apple.user.456");
        loginUser.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task Handle_MissingBothIdTokenAndCode_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        var handler = new AppleGrantHandler(appleProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Missing both id_token and code");
    }

    [Fact]
    public async Task Handle_InvalidCode_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        appleProvider.Setup(o => o.ExchangeCodeForTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync("");

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("code", "invalid.code");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();

        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Code invalid or expired");
    }

    [Fact]
    public async Task Handle_InvalidIdToken_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((false, null));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "invalid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();

        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Invalid APPLE token");
    }

    [Fact]
    public async Task Handle_ExistingUserByLogin_Test()
    {
        // Create existing user with Apple login
        var existingUser = new IdentityUser(Guid.NewGuid(), "existing.user", "existing@apple.com");
        await _identityUserManager.CreateAsync(existingUser);
        await _identityUserManager.AddLoginAsync(existingUser, new UserLoginInfo(
            GrantTypeConstants.APPLE, "apple.user.existing", GrantTypeConstants.APPLE));

        var appleProvider = new Mock<IAppleProvider>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "apple.user.existing"),
            new Claim(ClaimTypes.Email, "existing@apple.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((true, principal));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        var claimsPrincipal = context.HttpContext.User;
        claimsPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.ShouldBe(existingUser.Id.ToString());
    }

    [Fact]
    public async Task Handle_HistoricalCompatibility_ByName_Test()
    {
        // Create existing user with historical naming convention
        var email = "historical@apple.com";
        var historicalName = email + "@" + GrantTypeConstants.APPLE;
        var existingUser = new IdentityUser(Guid.NewGuid(), historicalName, email);
        await _identityUserManager.CreateAsync(existingUser);

        var appleProvider = new Mock<IAppleProvider>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "apple.user.historical"),
            new Claim(ClaimTypes.Email, email)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((true, principal));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        // Verify the login was added to existing user
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.APPLE, "apple.user.historical");
        loginUser.Id.ShouldBe(existingUser.Id);
    }

    [Fact]
    public async Task Handle_HistoricalCompatibility_ByEmail_Test()
    {
        // Create existing user with email only
        var email = "emailonly@apple.com";
        var existingUser = new IdentityUser(Guid.NewGuid(), "existing.by.email", email);
        await _identityUserManager.CreateAsync(existingUser);

        var appleProvider = new Mock<IAppleProvider>();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "apple.user.emailonly"),
            new Claim(ClaimTypes.Email, email)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((true, principal));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        // Verify the login was added to existing user
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.APPLE, "apple.user.emailonly");
        loginUser.Id.ShouldBe(existingUser.Id);
    }

    [Fact]
    public async Task Handle_PrivateRelayEmail_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        var subjectId = "apple.user.privaterelay";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subjectId),
            new Claim(ClaimTypes.Email, "privaterelay@privaterelay.appleid.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((true, principal));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        // The handler should process private relay emails correctly
        var user = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.APPLE, subjectId);
        user.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_NoEmail_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        var subjectId = "apple.user.noemail";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subjectId)
            // No email claim
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ReturnsAsync((true, principal));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        var user = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.APPLE, subjectId);
        user.ShouldNotBeNull();
        user.Email.ShouldEndWith("@apple.privaterelay.com");
    }

    [Fact]
    public async Task Handle_Exception_Test()
    {
        var appleProvider = new Mock<IAppleProvider>();
        appleProvider.Setup(o => o.ValidateAppleTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AppleAppOptions>()))
            .ThrowsAsync(new Exception("Validation failed"));

        var handler = new AppleGrantHandler(appleProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();

        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Internal server error");
    }
} 