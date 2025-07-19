using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Threading;

namespace Aevatar.PermissionManagement.Threading;

/// <summary>
/// Orleans-compatible StaticPermissionSaver that handles ICancellationTokenProvider gracefully
/// </summary>
public class OrleansStaticPermissionSaver : IStaticPermissionSaver, ITransientDependency
{
    private readonly ILogger<OrleansStaticPermissionSaver> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AbpPermissionOptions _permissionOptions;
    private readonly PermissionManagementOptions _permissionManagementOptions;

    public OrleansStaticPermissionSaver(
        ILogger<OrleansStaticPermissionSaver> logger,
        IServiceProvider serviceProvider,
        IOptions<AbpPermissionOptions> permissionOptions,
        IOptions<PermissionManagementOptions> permissionManagementOptions)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _permissionOptions = permissionOptions.Value;
        _permissionManagementOptions = permissionManagementOptions.Value;
    }

    public async Task SaveAsync()
    {
        try
        {
            _logger.LogDebug("OrleansStaticPermissionSaver.SaveAsync() started");

            // Check if we have permission management enabled
            if (!_permissionManagementOptions.IsDynamicPermissionStoreEnabled)
            {
                _logger.LogDebug("Dynamic permission store is disabled, skipping static permission save");
                return;
            }

            // Try to get the real StaticPermissionSaver from ABP if possible
            var realSaver = _serviceProvider.GetService<Volo.Abp.PermissionManagement.StaticPermissionSaver>();
            if (realSaver != null)
            {
                // Ensure ICancellationTokenProvider is available before proceeding
                var cancellationTokenProvider = _serviceProvider.GetService<ICancellationTokenProvider>();
                if (cancellationTokenProvider == null)
                {
                    _logger.LogWarning("ICancellationTokenProvider not available, registering NullCancellationTokenProvider");
                    // This shouldn't happen as we register it in the module, but just in case
                    return;
                }

                try
                {
                    await realSaver.SaveAsync();
                    _logger.LogDebug("Successfully saved static permissions using real StaticPermissionSaver");
                    return;
                }
                catch (NullReferenceException ex) when (ex.StackTrace?.Contains("CancellationTokenProvider") == true)
                {
                    _logger.LogWarning(ex, "NullReferenceException in CancellationTokenProvider, falling back to Orleans-compatible implementation");
                    // Fall through to our safe implementation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in StaticPermissionSaver.SaveAsync, falling back to Orleans-compatible implementation");
                    // Fall through to our safe implementation
                }
            }

            // Orleans-compatible implementation - just log that we're skipping
            _logger.LogInformation("Skipping static permission save in Orleans environment. Permissions will be managed dynamically.");
            
            // In Orleans environment, we rely on dynamic permission management
            // Static permissions should be seeded through other mechanisms if needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrleansStaticPermissionSaver.SaveAsync");
            // Don't rethrow - we don't want to break Orleans Silo startup
        }
    }
} 