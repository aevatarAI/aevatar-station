using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Permissions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.Permissions;

public interface IStatePermissionProvider
{
    Task<bool> SaveAllStatePermissionAsync();

    Task<bool> CheckPermissionAsync(string stateName);
}

public class StatePermissionProvider : IStatePermissionProvider, ISingletonDependency
{
    private readonly ILogger<StatePermissionProvider> _logger;
    private readonly IRepository<StatePermission, Guid> _statePermissionRepository;
    private readonly IConfiguration _configuration;
    private readonly IPermissionChecker _permissionChecker;

    public StatePermissionProvider(IRepository<StatePermission, Guid> statePermissionRepository,
        IPermissionChecker permissionChecker,
        ILogger<StatePermissionProvider> logger, IConfiguration configuration)
    {
        _statePermissionRepository = statePermissionRepository;
        _permissionChecker = permissionChecker;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SaveAllStatePermissionAsync()
    {
        var hostId = _configuration.GetValue<string>("Host:HostId");
        if (string.IsNullOrEmpty(hostId))
        {
            _logger.LogError("HostId not config");
            return false;
        }

        var statePermissionMap = PermissionHelper.GetAllStatePermissionInfos();
        if (statePermissionMap.IsNullOrEmpty())
        {
            _logger.LogInformation("no state permission mapping to save");
            return true;
        }

        try
        {
            var entities = new List<StatePermission>();
            foreach (var entry in statePermissionMap)
            {
                string permissionName = entry.Key;
                entities.Add(new StatePermission
                {
                    HostId = hostId,
                    Permission = permissionName,
                    StateName = entry.Value
                });
            }

            var existingRecords = await _statePermissionRepository
                .GetListAsync(x =>
                    x.HostId == hostId && entities.Select(e => e.Permission).Contains(x.Permission));


            var newEntities = entities
                .Where(e => !existingRecords.Any(ex =>
                    ex.Permission == e.Permission &&
                    ex.StateName == e.StateName))
                .ToList();

            if (newEntities.Any())
            {
                await _statePermissionRepository.InsertManyAsync(newEntities);
                _logger.LogInformation($"save statePermission {newEntities.Count} successfully");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "save statePermission error");
            return false;
        }
    }

    public async Task<bool> CheckPermissionAsync(string stateName)
    {
        var hostId = _configuration.GetValue<string>("Host:HostId");
        if (string.IsNullOrEmpty(hostId))
        {
            return false;
        }

        try
        {
            var permission = await _statePermissionRepository.FirstOrDefaultAsync(x =>
                x.HostId == hostId &&
                x.StateName == stateName);

            if (permission == null)
            {
                _logger.LogDebug($" not found permissionName: -> {stateName}");
                return true;
            }

            return await _permissionChecker.IsGrantedAsync(permission.Permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"permission check failed: {stateName}");
            return false;
        }
    }
}