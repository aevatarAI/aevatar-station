using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Options;
using Aevatar.Permissions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace Aevatar.OpenIddict;

/* Creates initial data that is needed to property run the application
 * and make client-to-server communication possible.
 */
public class OpenIddictDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IOpenIddictApplicationRepository _openIddictApplicationRepository;
    private readonly IAbpApplicationManager _applicationManager;
    private readonly IOpenIddictScopeRepository _openIddictScopeRepository;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly IStringLocalizer<OpenIddictResponse> L;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly UsersOptions _usersOptions;
    private readonly IPermissionManager _permissionManager;

    public OpenIddictDataSeedContributor(
        IConfiguration configuration,
        IOpenIddictApplicationRepository openIddictApplicationRepository,
        IAbpApplicationManager applicationManager,
        IOpenIddictScopeRepository openIddictScopeRepository,
        IOpenIddictScopeManager scopeManager,
        IPermissionDataSeeder permissionDataSeeder,
        IdentityUserManager identityUserManager,
        IOptionsSnapshot<UsersOptions> userOptions,
        IStringLocalizer<OpenIddictResponse> l ,
        IPermissionManager permissionManager,
        IdentityRoleManager roleManager)
    {
        _configuration = configuration;
        _openIddictApplicationRepository = openIddictApplicationRepository;
        _applicationManager = applicationManager;
        _openIddictScopeRepository = openIddictScopeRepository;
        _scopeManager = scopeManager;
        _permissionDataSeeder = permissionDataSeeder;
        L = l;
        _identityUserManager = identityUserManager;
        _usersOptions = userOptions.Value;
        _permissionManager = permissionManager;
        _roleManager = roleManager;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await CreateScopesAsync();
        await CreateApplicationsAsync();
        await SeedAdminUserAsync();
        await SeedMCPGatewayRolesAndPermissionsAsync();
    }

    private async Task CreateScopesAsync()
    {
        if (await _openIddictScopeRepository.FindByNameAsync("Aevatar") == null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor {
                Name = "Aevatar", DisplayName = "Aevatar API", Resources = { "Aevatar" }
            });
        }
    }
    
    private async Task SeedAdminUserAsync()
    {
        var adminUser = await _identityUserManager.FindByNameAsync("admin");
        if (adminUser != null)
        {
            var adminPassword = _usersOptions.AdminPassword;
            var token = await _identityUserManager.GeneratePasswordResetTokenAsync(adminUser);
            var result = await _identityUserManager.ResetPasswordAsync(adminUser, token, adminPassword);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to set admin password: " + result.Errors.Select(e => e.Description).Aggregate((errors, error) => errors + ", " + error));
            }
            await SeedPermissionsFromConfigurationAsync();
        }
    }

    private async Task SeedPermissionsFromConfigurationAsync()
    {
        var permissionMappings = _configuration.GetSection("PermissionMappings").Get<Dictionary<string, List<string>>>();
        if (permissionMappings == null) return;
        int count = 0;
        foreach (var mapping in permissionMappings)
        {
            var roleName = mapping.Key;
            var permissions = mapping.Value;

            var role = await _roleManager.RoleExistsAsync(roleName);
            if (!role)
            {
                var identityRole = new IdentityRole(Guid.NewGuid(), roleName);
                identityRole.IsPublic = true;
                identityRole.IsStatic = true;
                if (count == 0 )
                {
                    identityRole.IsDefault = true;
                }
                count++;
                var result = await _roleManager.CreateAsync(identityRole);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}': " +
                                        $"{string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            foreach (var permission in permissions)
            {
                await _permissionManager.SetAsync(
                    permission,
                    RolePermissionValueProvider.ProviderName ,
                    roleName,
                    true 
                );
            }
        }
    }

    private async Task CreateApplicationsAsync()
    {
        var commonScopes = new List<string> {
            OpenIddictConstants.Permissions.Scopes.Address,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            "Aevatar"
        };

        var configurationSection = _configuration.GetSection("OpenIddict:Applications");

        // Swagger Client
        var swaggerClientId = configurationSection["Aevatar_Swagger:ClientId"];
        if (!swaggerClientId.IsNullOrWhiteSpace())
        {
            var swaggerRootUrl = configurationSection["Aevatar_Swagger:RootUrl"]?.TrimEnd('/');

            await CreateApplicationAsync(
                name: swaggerClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "Swagger Application",
                secret: null,
                grantTypes: new List<string> { OpenIddictConstants.GrantTypes.AuthorizationCode, },
                scopes: commonScopes,
                redirectUri: $"{swaggerRootUrl}/swagger/oauth2-redirect.html",
                clientUri: swaggerRootUrl
            );
        }
        
        var authServerClientId = configurationSection["AevatarAuthServer:ClientId"];
        if (!authServerClientId.IsNullOrWhiteSpace())
        {
            var authServerRootUrl = configurationSection["AevatarAuthServer:RootUrl"]?.TrimEnd('/');

            await CreateApplicationAsync(
                name: authServerClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "AevatarAuthServer Application",
                secret: null,
                grantTypes: new List<string>
                {
                    OpenIddictConstants.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.GrantTypes.Password,
                    OpenIddictConstants.GrantTypes.ClientCredentials,
                    OpenIddictConstants.GrantTypes.RefreshToken,
                    GrantTypeConstants.SIGNATURE,
                    GrantTypeConstants.LOGIN,
                    GrantTypeConstants.GOOGLE,
                    GrantTypeConstants.APPLE,
                    GrantTypeConstants.Github
                },
                scopes: commonScopes,
                redirectUri: authServerRootUrl,
                clientUri: authServerRootUrl,
                postLogoutRedirectUri: authServerRootUrl
            );
        }

        // HttpApi Client for MCP Gateway API access
        await CreateApplicationAsync(
            name: "Aevatar_HttpApi",
            type: OpenIddictConstants.ClientTypes.Confidential,
            consentType: OpenIddictConstants.ConsentTypes.Implicit,
            displayName: "Aevatar HttpApi Client",
            secret: "1q2w3e*",
            grantTypes: new List<string>
            {
                OpenIddictConstants.GrantTypes.ClientCredentials,
                OpenIddictConstants.GrantTypes.Password,
                OpenIddictConstants.GrantTypes.RefreshToken
            },
            scopes: commonScopes,
            clientUri: "http://localhost:7002",
            redirectUri: "http://localhost:7002"
        );

        // MCP Gateway Test Client
        await CreateApplicationAsync(
            name: "MCPGateway_Test_Client", 
            type: OpenIddictConstants.ClientTypes.Confidential,
            consentType: OpenIddictConstants.ConsentTypes.Implicit,
            displayName: "MCP Gateway Test Client",
            secret: "mcp-gateway-test-secret-123",
            grantTypes: new List<string>
            {
                OpenIddictConstants.GrantTypes.ClientCredentials
            },
            scopes: commonScopes,
            clientUri: "http://localhost:8000"
        );
    }

    private async Task CreateApplicationAsync(
        [NotNull] string name,
        [NotNull] string type,
        [NotNull] string consentType,
        string displayName,
        string? secret,
        List<string> grantTypes,
        List<string> scopes,
        string? clientUri = null,
        string? redirectUri = null,
        string? postLogoutRedirectUri = null,
        List<string>? permissions = null)
    {
        if (!string.IsNullOrEmpty(secret) && string.Equals(type, OpenIddictConstants.ClientTypes.Public,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(L["NoClientSecretCanBeSetForPublicApplications"]);
        }

        if (string.IsNullOrEmpty(secret) && string.Equals(type, OpenIddictConstants.ClientTypes.Confidential,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(L["TheClientSecretIsRequiredForConfidentialApplications"]);
        }

        var client = await _openIddictApplicationRepository.FindByClientIdAsync(name);

        var application = new AbpApplicationDescriptor {
            ClientId = name,
            ClientType = type,
            ClientSecret = secret,
            ConsentType = consentType,
            DisplayName = displayName,
            ClientUri = clientUri,
        };

        Check.NotNullOrEmpty(grantTypes, nameof(grantTypes));
        Check.NotNullOrEmpty(scopes, nameof(scopes));

        if (new[] { OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.Implicit }.All(
                grantTypes.Contains))
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);

            if (string.Equals(type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
            }
        }

        if (!redirectUri.IsNullOrWhiteSpace() || !postLogoutRedirectUri.IsNullOrWhiteSpace())
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);
        }

        var buildInGrantTypes = new[] {
            OpenIddictConstants.GrantTypes.Implicit, OpenIddictConstants.GrantTypes.Password,
            OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.ClientCredentials,
            OpenIddictConstants.GrantTypes.DeviceCode, OpenIddictConstants.GrantTypes.RefreshToken
        };

        foreach (var grantType in grantTypes)
        {
            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }

            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode ||
                grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            }

            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode ||
                grantType == OpenIddictConstants.GrantTypes.ClientCredentials ||
                grantType == OpenIddictConstants.GrantTypes.Password ||
                grantType == OpenIddictConstants.GrantTypes.RefreshToken ||
                grantType == OpenIddictConstants.GrantTypes.DeviceCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
            }

            if (grantType == OpenIddictConstants.GrantTypes.ClientCredentials)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Implicit);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Password)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            }

            if (grantType == OpenIddictConstants.GrantTypes.RefreshToken)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }

            if (grantType == OpenIddictConstants.GrantTypes.DeviceCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.DeviceCode);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Device);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdToken);
                if (string.Equals(type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
            }

            if (!buildInGrantTypes.Contains(grantType))
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.GrantType + grantType);
            }
        }

        var buildInScopes = new[] {
            OpenIddictConstants.Permissions.Scopes.Address, OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone, OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles
        };

        foreach (var scope in scopes)
        {
            if (buildInScopes.Contains(scope))
            {
                application.Permissions.Add(scope);
            }
            else
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            }
        }

        if (redirectUri != null)
        {
            if (!redirectUri.IsNullOrEmpty())
            {
                if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString())
                {
                    throw new BusinessException(L["InvalidRedirectUri", redirectUri]);
                }

                if (application.RedirectUris.All(x => x != uri))
                {
                    application.RedirectUris.Add(uri);
                }
            }
        }

        if (postLogoutRedirectUri != null)
        {
            if (!postLogoutRedirectUri.IsNullOrEmpty())
            {
                if (!Uri.TryCreate(postLogoutRedirectUri, UriKind.Absolute, out var uri) ||
                    !uri.IsWellFormedOriginalString())
                {
                    throw new BusinessException(L["InvalidPostLogoutRedirectUri", postLogoutRedirectUri]);
                }

                if (application.PostLogoutRedirectUris.All(x => x != uri))
                {
                    application.PostLogoutRedirectUris.Add(uri);
                }
            }
        }

        if (permissions != null)
        {
            await _permissionDataSeeder.SeedAsync(
                ClientPermissionValueProvider.ProviderName,
                name,
                permissions,
                null
            );
        }

        if (client == null)
        {
            await _applicationManager.CreateAsync(application);
            return;
        }

        if (!HasSameRedirectUris(client, application))
        {
            client.RedirectUris = JsonSerializer.Serialize(application.RedirectUris.Select(q => q.ToString().TrimEnd('/')));
            client.PostLogoutRedirectUris = JsonSerializer.Serialize(application.PostLogoutRedirectUris.Select(q => q.ToString().TrimEnd('/')));

            await _applicationManager.UpdateAsync(client.ToModel());
        }

        if (!HasSameScopes(client, application))
        {
            client.Permissions = JsonSerializer.Serialize(application.Permissions.Select(q => q.ToString()));
            await _applicationManager.UpdateAsync(client.ToModel());
        }
    }

    private bool HasSameRedirectUris(OpenIddictApplication existingClient, AbpApplicationDescriptor application)
    {
        return existingClient.RedirectUris == JsonSerializer.Serialize(application.RedirectUris.Select(q => q.ToString().TrimEnd('/')));
    }

    private bool HasSameScopes(OpenIddictApplication existingClient, AbpApplicationDescriptor application)
    {
        return existingClient.Permissions == JsonSerializer.Serialize(application.Permissions.Select(q => q.ToString().TrimEnd('/')));
    }

    /// <summary>
    /// Seed MCP Gateway roles and permissions
    /// </summary>
    private async Task SeedMCPGatewayRolesAndPermissionsAsync()
    {
        // Create MCP Gateway Admin role
        const string mcpGatewayAdminRole = "MCPGatewayAdmin";
        if (!await _roleManager.RoleExistsAsync(mcpGatewayAdminRole))
        {
            var adminRole = new IdentityRole(Guid.NewGuid(), mcpGatewayAdminRole)
            {
                IsPublic = true,
                IsStatic = true
            };

            var result = await _roleManager.CreateAsync(adminRole);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create role '{mcpGatewayAdminRole}': " +
                                    $"{string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Assign all MCP Gateway permissions to the admin role
        var mcpGatewayPermissions = new[]
        {
            MCPGatewayPermissions.Adapters.Default,
            MCPGatewayPermissions.Adapters.Create,
            MCPGatewayPermissions.Adapters.Read,
            MCPGatewayPermissions.Adapters.Update,
            MCPGatewayPermissions.Adapters.Delete,
            MCPGatewayPermissions.Adapters.ManageAll,
            MCPGatewayPermissions.Adapters.ViewMetrics,
            MCPGatewayPermissions.Adapters.TestConnection,
            MCPGatewayPermissions.Adapters.ViewLogs,
            MCPGatewayPermissions.Gateway.Default,
            MCPGatewayPermissions.Gateway.ViewHealth,
            MCPGatewayPermissions.Gateway.ViewConfiguration,
            MCPGatewayPermissions.Gateway.UpdateConfiguration,
            MCPGatewayPermissions.Gateway.ViewSystemMetrics,
            MCPGatewayPermissions.Gateway.Manage,
            MCPGatewayPermissions.Gateway.ViewAuditLogs,
            MCPGatewayPermissions.Sessions.Default,
            MCPGatewayPermissions.Sessions.View,
            MCPGatewayPermissions.Sessions.Terminate,
            MCPGatewayPermissions.Sessions.ViewDetails,
            MCPGatewayPermissions.Sessions.ManageRouting
        };

        foreach (var permission in mcpGatewayPermissions)
        {
            await _permissionManager.SetAsync(
                permission,
                RolePermissionValueProvider.ProviderName,
                mcpGatewayAdminRole,
                true);
        }

        // Assign MCP Gateway permissions to HttpApi client
        const string httpApiClientName = "Aevatar_HttpApi";
        foreach (var permission in mcpGatewayPermissions)
        {
            await _permissionManager.SetAsync(
                permission,
                ClientPermissionValueProvider.ProviderName,
                httpApiClientName,
                true);
        }

        // Also assign to test client
        const string testClientName = "MCPGateway_Test_Client";
        foreach (var permission in mcpGatewayPermissions)
        {
            await _permissionManager.SetAsync(
                permission,
                ClientPermissionValueProvider.ProviderName,
                testClientName,
                true);
        }

        // Assign the MCP Gateway Admin role to the admin user (if exists)
        var adminUser = await _identityUserManager.FindByNameAsync("admin");
        if (adminUser != null && !await _identityUserManager.IsInRoleAsync(adminUser, mcpGatewayAdminRole))
        {
            await _identityUserManager.AddToRoleAsync(adminUser, mcpGatewayAdminRole);
        }
    }
}
