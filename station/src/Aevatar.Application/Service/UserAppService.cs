using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Common;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Service;


public interface IUserAppService
{
    Task ResetPasswordAsync(string userName, string newPassword);
    Task RegisterClientAuthentication(string clientId, string clientSecret);
    Guid GetCurrentUserId();
    Task  GrantClientPermissionsAsync(string clientId);
    Task DeleteClientAndAuthentication(string clientId);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserAppService : IdentityUserAppService, IUserAppService
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ILogger<UserAppService> _logger;
    private readonly IPermissionManager _permissionManager;

    public UserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IOptions<IdentityOptions> identityOptions,
        IOpenIddictApplicationManager applicationManager,
        ILogger<UserAppService> logger,
        IPermissionChecker permissionChecker,
        IPermissionManager permissionManager)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
        _applicationManager = applicationManager;
        _logger = logger;
        _permissionManager = permissionManager;
    }


    public async   Task RegisterClientAuthentication(string clientId, string clientSecret)
    {
        if (await _applicationManager.FindByClientIdAsync(clientId) != null)
        {
            throw new UserFriendlyException("A app with the same ID already exists.");
        }


        var openIddictApplicationDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            DisplayName = "Aevatar Client",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Prefixes.Scope + "Aevatar",
                OpenIddictConstants.Permissions.ResponseTypes.IdToken
            },
        };
        await SetClientPermissionsAsync(clientId);

        await _applicationManager.CreateAsync(openIddictApplicationDescriptor);
      
    }
  
    private async Task SetClientPermissionsAsync(string clientId)
    {
        var permissions= await  _permissionManager.GetAllAsync(RolePermissionValueProvider.ProviderName, AevatarPermissions.DeveloperManager);

        foreach (var permission in permissions)
        {
            if (permission.IsGranted)
            {
                await _permissionManager.SetForClientAsync(clientId,permission.Name,true);
            }
        }
    }

    public async Task ResetPasswordAsync(string userName, string newPassword)
    {
        if (CurrentUser == null || CurrentUser.Id == null)
        {
            throw new UserFriendlyException("CurrentUser is null");
        }

        if (CurrentUser.UserName != userName)
        {
            _logger.LogInformation($"[ResetPasswordAsync] CurrentUser.UserName:{CurrentUser.UserName} userName:{userName}");
            throw new UserFriendlyException("Can only reset your own password");
        }

        var identityUser = await UserManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (identityUser == null)
        {
            throw new UserFriendlyException("user not found.");
        }

        var token = await UserManager.GeneratePasswordResetTokenAsync(identityUser);
        var result = await UserManager.ResetPasswordAsync(identityUser, token, newPassword);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException("reset user password failed." + result.Errors.Select(e => e.Description)
                .Aggregate((errors, error) => errors + ", " + error));
        }
    }
   
    public Guid GetCurrentUserId()
    {
        if (!CurrentUser.UserName.IsNullOrEmpty())
        {
            return GuidUtil.StringToGuid(CurrentUser.UserName);
        }
        
        var clientId =  CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;
        return GuidUtil.StringToGuid(clientId);
    }

    public async Task GrantClientPermissionsAsync(string clientId)
    {
        await SetClientPermissionsAsync(clientId);
    }

    public async Task DeleteClientAndAuthentication(string clientId)
    {
       var application = await _applicationManager.FindByClientIdAsync(clientId);
        if (application == null)
        {
            throw new UserFriendlyException("A app with the same ID already exists.");
        }
        await _applicationManager.DeleteAsync(application);
        
        var permissions= await  _permissionManager.GetAllAsync(RolePermissionValueProvider.ProviderName, AevatarPermissions.DeveloperManager);
        foreach (var permission in permissions)
        {
            if (permission.IsGranted)
            {
                await _permissionManager.DeleteAsync(ClientPermissionValueProvider.ProviderName, clientId);
            }
        }
    }
}