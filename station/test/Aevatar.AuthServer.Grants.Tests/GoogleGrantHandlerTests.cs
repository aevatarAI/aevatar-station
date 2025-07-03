using System.Collections.Specialized;
using System.Security.Claims;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Google.Apis.Auth;
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

public abstract class GoogleGrantHandlerTests<TStartupModule> : AevatarAuthServerGrantsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IdentityRoleManager _identityRoleManager;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly ILogger<GoogleGrantHandler> _logger;

    protected GoogleGrantHandlerTests()
    {
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _serviceProvider = GetRequiredService<IServiceProvider>();
        _identityRoleManager = GetRequiredService<IdentityRoleManager>();
        _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _logger = GetRequiredService<ILogger<GoogleGrantHandler>>();
    }

    [Fact]
    public async Task Handle_Success_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.123",
            Email = "test@google.com",
            Name = "Test User",
            GivenName = "Test",
            FamilyName = "User"
        };
        
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("test.client.id");
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.google.token");
        collection.Add("source", "web");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        var basicRole = await _identityRoleManager.FindByNameAsync(Permissions.AevatarPermissions.BasicUser);
        var user = await _identityUserManager.FindByEmailAsync("test@google.com");
        user.ShouldNotBeNull();
        user.Roles.ShouldContain(o => o.RoleId == basicRole.Id);
        
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.123");
        loginUser.Id.ShouldBe(user.Id);
        
        var claimsPrincipal = context.HttpContext.User;
        claimsPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.ShouldBe(user.Id.ToString());
    }

    [Fact]
    public async Task Handle_MissingIdToken_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);

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
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Missing id_token parameter");
    }

    [Fact]
    public async Task Handle_ClientIdNotFound_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("");

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        collection.Add("source", "unknown");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();

        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("client Id not found");
    }

    [Fact]
    public async Task Handle_InvalidGoogleToken_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("test.client.id");
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((GoogleJsonWebSignature.Payload)null);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);

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
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidGrant);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Invalid Google token");
    }

    [Fact]
    public async Task Handle_ExistingUserByLogin_Test()
    {
        // Create existing user with Google login
        var existingUser = new IdentityUser(Guid.NewGuid(), "existing.google.user", "existing@google.com");
        await _identityUserManager.CreateAsync(existingUser);
        await _identityUserManager.AddLoginAsync(existingUser, new UserLoginInfo(
            GrantTypeConstants.GOOGLE, "google.user.existing", GrantTypeConstants.GOOGLE));

        var googleProvider = new Mock<IGoogleProvider>();
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.existing",
            Email = "existing@google.com"
        };
        
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("test.client.id");
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
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
        var email = "historical@google.com";
        var historicalName = email + "@" + GrantTypeConstants.GOOGLE;
        var existingUser = new IdentityUser(Guid.NewGuid(), historicalName, email);
        await _identityUserManager.CreateAsync(existingUser);

        var googleProvider = new Mock<IGoogleProvider>();
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.historical",
            Email = email
        };
        
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("test.client.id");
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
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
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.historical");
        loginUser.Id.ShouldBe(existingUser.Id);
    }

    [Fact]
    public async Task Handle_HistoricalCompatibility_ByEmail_Test()
    {
        // Create existing user with email only
        var email = "emailonly@google.com";
        var existingUser = new IdentityUser(Guid.NewGuid(), "existing.by.email", email);
        await _identityUserManager.CreateAsync(existingUser);

        var googleProvider = new Mock<IGoogleProvider>();
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.emailonly",
            Email = email
        };
        
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("test.client.id");
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
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
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.emailonly");
        loginUser.Id.ShouldBe(existingUser.Id);
    }

    [Fact]
    public async Task Handle_NewUser_WithoutEmail_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.noemail"
            // No email
        };
        
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("test.client.id");
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        var user = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.noemail");
        user.ShouldNotBeNull();
        user.Email.ShouldEndWith("@google.com");
    }

    [Fact]
    public async Task Handle_NewUser_WithEmptyEmail_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.emptyemail",
            Email = ""
        };
        
        googleProvider.Setup(o => o.GetClientIdAsync(It.IsAny<string>())).ReturnsAsync("test.client.id");
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        var user = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.emptyemail");
        user.ShouldNotBeNull();
        user.Email.ShouldEndWith("@google.com");
    }

    [Fact]
    public async Task Handle_DifferentSources_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        
        // Test iOS source
        googleProvider.Setup(o => o.GetClientIdAsync("ios")).ReturnsAsync("ios.client.id");
        
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.ios",
            Email = "ios@google.com"
        };
        
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), "ios.client.id"))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        collection.Add("source", "ios");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        var user = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.ios");
        user.ShouldNotBeNull();
        user.Email.ShouldBe("ios@google.com");
    }

    [Fact]
    public async Task Handle_Android_Source_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        
        // Test Android source
        googleProvider.Setup(o => o.GetClientIdAsync("android")).ReturnsAsync("android.client.id");
        
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.android",
            Email = "android@google.com"
        };
        
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), "android.client.id"))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        collection.Add("source", "android");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        var user = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.android");
        user.ShouldNotBeNull();
        user.Email.ShouldBe("android@google.com");
    }

    [Fact]
    public async Task Handle_DefaultSource_Test()
    {
        var googleProvider = new Mock<IGoogleProvider>();
        
        // Test default source (web)
        googleProvider.Setup(o => o.GetClientIdAsync("web")).ReturnsAsync("web.client.id");
        
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "google.user.web",
            Email = "web@google.com"
        };
        
        googleProvider.Setup(o => o.ValidateGoogleTokenAsync(It.IsAny<string>(), "web.client.id"))
            .ReturnsAsync(payload);

        var handler = new GoogleGrantHandler(googleProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("id_token", "valid.token");
        collection.Add("source", "web");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        var user = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.GOOGLE, "google.user.web");
        user.ShouldNotBeNull();
        user.Email.ShouldBe("web@google.com");
    }
} 