using System.Collections.Specialized;
using System.Security.Claims;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Elastic.Clients.Elasticsearch.Nodes;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Xunit;

namespace Aevatar.AuthServer.Grants;

public abstract class GithubGrantHandlerTests<TStartupModule> : AevatarAuthServerGrantsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IdentityRoleManager _identityRoleManager;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly ILogger<GithubGrantHandler> _logger;

    protected GithubGrantHandlerTests()
    {
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _serviceProvider = GetRequiredService<IServiceProvider>();
        _identityRoleManager = GetRequiredService<IdentityRoleManager>();
        _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _logger = GetRequiredService<ILogger<GithubGrantHandler>>();
    }

    [Fact]
    public async Task Handle_Success_Test()
    {
        var githubProvider = new Mock<IGithubProvider>();
        var githubUser = new GithubUser
        {
            Id = 1000,
            Email = "test@gihub.com"
        };
        githubProvider.Setup(o => o.GetUserInfoAsync(It.IsAny<string>())).ReturnsAsync(githubUser);

        var handler = new GithubGrantHandler(githubProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("code","code");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        await handler.HandleAsync(context);

        var basicRole = await _identityRoleManager.FindByNameAsync(Permissions.AevatarPermissions.BasicUser);
        var user = await _identityUserManager.FindByEmailAsync("test@gihub.com");
        user.Roles.ShouldContain(o => o.RoleId == basicRole.Id);
        
        var loginUser = await _identityUserManager.FindByLoginAsync(GrantTypeConstants.Github, githubUser.Id.ToString());
        loginUser.Id.ShouldBe(user.Id);
        
        var claimsPrincipal = context.HttpContext.User;
        claimsPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.ShouldBe(user.Id.ToString()); 
    }

    [Fact]
    public async Task Handle_MissingCode_Test()
    {
        var githubProvider = new Mock<IGithubProvider>();
        var githubUser = new GithubUser
        {
            Id = 1000,
            Email = "test@gihub.com"
        };
        githubProvider.Setup(o => o.GetUserInfoAsync(It.IsAny<string>())).ReturnsAsync(githubUser);

        var handler = new GithubGrantHandler(githubProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.GetType().ShouldBe(typeof(ForbidResult));
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Missing code parameter");
    }
    
    [Fact]
    public async Task Handle_InvaliCode_Test()
    {
        var githubProvider = new Mock<IGithubProvider>();
        githubProvider.Setup(o => o.GetUserInfoAsync(It.IsAny<string>())).ReturnsAsync((GithubUser)null);

        var handler = new GithubGrantHandler(githubProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("code","code");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.GetType().ShouldBe(typeof(ForbidResult));

        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Invalid code");

    }
}