using System.Collections.Specialized;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace Aevatar.AuthServer.Grants;

public abstract class SignatureGrantHandlerTests<TStartupModule> : AevatarAuthServerGrantsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IdentityRoleManager _identityRoleManager;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly ILogger<SignatureGrantHandler> _logger;

    protected SignatureGrantHandlerTests()
    {
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _serviceProvider = GetRequiredService<IServiceProvider>();
        _identityRoleManager = GetRequiredService<IdentityRoleManager>();
        _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _logger = GetRequiredService<ILogger<SignatureGrantHandler>>();
    }

    [Fact]
    public async Task Handle_Success_NewUser_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var walletAddress = "ELF_wallet_address_12345";
        
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<string>());
        walletProvider.Setup(o => o.VerifySignatureAndParseWalletAddressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(walletAddress);

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "valid_signature");
        collection.Add("chain_id", "AELF");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        var basicRole = await _identityRoleManager.FindByNameAsync(Permissions.AevatarPermissions.BasicUser);
        var user = await _identityUserManager.FindByNameAsync(walletAddress);
        user.ShouldNotBeNull();
        user.Roles.ShouldContain(o => o.RoleId == basicRole.Id);
        
        var claimsPrincipal = context.HttpContext.User;
        claimsPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.ShouldBe(user.Id.ToString());
    }

    [Fact]
    public async Task Handle_Success_ExistingUser_Test()
    {
        var walletAddress = "ELF_existing_wallet_12345";
        
        // Create existing user
        var existingUser = new IdentityUser(Guid.NewGuid(), walletAddress, $"{Guid.NewGuid():N}@ABP.IO");
        await _identityUserManager.CreateAsync(existingUser);
        await _identityUserManager.SetRolesAsync(existingUser, [Permissions.AevatarPermissions.BasicUser]);

        var walletProvider = new Mock<IWalletLoginProvider>();
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<string>());
        walletProvider.Setup(o => o.VerifySignatureAndParseWalletAddressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(walletAddress);

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "valid_signature");
        collection.Add("chain_id", "AELF");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        var claimsPrincipal = context.HttpContext.User;
        claimsPrincipal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.ShouldBe(existingUser.Id.ToString());
    }

    [Fact]
    public async Task Handle_Success_ExistingUserWithoutRoles_Test()
    {
        var walletAddress = "ELF_no_roles_wallet_12345";
        
        // Create existing user without roles
        var existingUser = new IdentityUser(Guid.NewGuid(), walletAddress, $"{Guid.NewGuid():N}@ABP.IO");
        await _identityUserManager.CreateAsync(existingUser);

        var walletProvider = new Mock<IWalletLoginProvider>();
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<string>());
        walletProvider.Setup(o => o.VerifySignatureAndParseWalletAddressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(walletAddress);

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "valid_signature");
        collection.Add("chain_id", "AELF");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        
        // Verify roles were assigned
        var user = await _identityUserManager.FindByNameAsync(walletAddress);
        var basicRole = await _identityRoleManager.FindByNameAsync(Permissions.AevatarPermissions.BasicUser);
        user.Roles.ShouldContain(o => o.RoleId == basicRole.Id);
    }

    [Fact]
    public async Task Handle_MissingPublicKey_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var errors = new List<string> { "invalid parameter publish_key." };
        walletProvider.Setup(o => o.CheckParams("", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(errors);
        walletProvider.Setup(o => o.GetErrorMessage(errors))
            .Returns("invalid parameter publish_key.");

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("signature", "valid_signature");
        collection.Add("chain_id", "AELF");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("invalid parameter publish_key.");
    }

    [Fact]
    public async Task Handle_MissingSignature_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var errors = new List<string> { "invalid parameter signature." };
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), "", It.IsAny<string>(), It.IsAny<string>()))
            .Returns(errors);
        walletProvider.Setup(o => o.GetErrorMessage(errors))
            .Returns("invalid parameter signature.");

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("chain_id", "AELF");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("invalid parameter signature.");
    }

    [Fact]
    public async Task Handle_MissingChainId_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var errors = new List<string> { "invalid parameter chain_id." };
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), "", It.IsAny<string>()))
            .Returns(errors);
        walletProvider.Setup(o => o.GetErrorMessage(errors))
            .Returns("invalid parameter chain_id.");

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "valid_signature");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("invalid parameter chain_id.");
    }

    [Fact]
    public async Task Handle_MissingPlainText_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var errors = new List<string> { "invalid parameter plainText." };
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ""))
            .Returns(errors);
        walletProvider.Setup(o => o.GetErrorMessage(errors))
            .Returns("invalid parameter plainText.");

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "valid_signature");
        collection.Add("chain_id", "AELF");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("invalid parameter plainText.");
    }

    [Fact]
    public async Task Handle_MultipleParameterErrors_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var errors = new List<string> 
        { 
            "invalid parameter publish_key.", 
            "invalid parameter signature.",
            "invalid parameter chain_id."
        };
        walletProvider.Setup(o => o.CheckParams("", "", "", It.IsAny<string>()))
            .Returns(errors);
        walletProvider.Setup(o => o.GetErrorMessage(errors))
            .Returns("invalid parameter publish_key.; invalid parameter signature.; invalid parameter chain_id.");

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription]
            .ShouldBe("invalid parameter publish_key.; invalid parameter signature.; invalid parameter chain_id.");
    }

    [Fact]
    public async Task Handle_UserFriendlyException_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<string>());
        walletProvider.Setup(o => o.VerifySignatureAndParseWalletAddressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UserFriendlyException("Signature validation failed"));

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "invalid_signature");
        collection.Add("chain_id", "AELF");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();

        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("Signature validation failed");
    }

    [Fact]
    public async Task Handle_GeneralException_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<string>());
        walletProvider.Setup(o => o.VerifySignatureAndParseWalletAddressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("General verification error"));

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "invalid_signature");
        collection.Add("chain_id", "AELF");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        await Should.ThrowAsync<Exception>(async () => await handler.HandleAsync(context));
    }

    [Fact]
    public async Task Handle_WithCaHash_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var walletAddress = "ELF_ca_wallet_address_12345";
        
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<string>());
        walletProvider.Setup(o => o.VerifySignatureAndParseWalletAddressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(walletAddress);

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        collection.Add("pubkey", "valid_public_key");
        collection.Add("signature", "valid_signature");
        collection.Add("chain_id", "AELF");
        collection.Add("ca_hash", "valid_ca_hash");
        collection.Add("plain_text", "valid_plain_text");
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));
        
        var result = await handler.HandleAsync(context);

        result.ShouldBeOfType<SignInResult>();
        var user = await _identityUserManager.FindByNameAsync(walletAddress);
        user.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_NameProperty_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);
        
        handler.Name.ShouldBe(GrantTypeConstants.SIGNATURE);
    }

    [Fact]
    public async Task Handle_NullParameters_Test()
    {
        var walletProvider = new Mock<IWalletLoginProvider>();
        var errors = new List<string> 
        { 
            "invalid parameter publish_key.", 
            "invalid parameter signature.",
            "invalid parameter chain_id.",
            "invalid parameter plainText."
        };
        walletProvider.Setup(o => o.CheckParams(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(errors);
        walletProvider.Setup(o => o.GetErrorMessage(errors))
            .Returns("All parameters are invalid.");

        var handler = new SignatureGrantHandler(walletProvider.Object, _logger);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        var collection = new NameValueCollection();
        // No parameters added
        var context = new ExtensionGrantContext(httpContext, new OpenIddictRequest(collection));

        var result = await handler.HandleAsync(context);
        result.ShouldBeOfType<ForbidResult>();
        
        var forbidResult = result as ForbidResult;
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldBe(OpenIddictConstants.Errors.InvalidRequest);
        forbidResult.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription].ShouldBe("All parameters are invalid.");
    }
} 