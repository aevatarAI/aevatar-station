using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Aevatar.ApiKeys;
using Aevatar.APIKeys;
using Aevatar.Common;
using Aevatar.Organizations;
using Aevatar.Permissions;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Aevatar.Service;

public class ProjectAppIdService : IProjectAppIdService, ITransientDependency
{
    private readonly IProjectAppIdRepository _projectAppIdRepository;
    private readonly ILogger<ProjectAppIdService> _logger;
    private readonly IOrganizationPermissionChecker _organizationPermission;
    private readonly IUserAppService _userAppService;
    private readonly IdentityUserManager _identityUserManager;

    public ProjectAppIdService(IProjectAppIdRepository projectAppIdRepository, ILogger<ProjectAppIdService> logger,
        IOrganizationPermissionChecker organizationPermission, IUserAppService userAppService, IdentityUserManager identityUserManager)
    {
        _projectAppIdRepository = projectAppIdRepository;
        _logger = logger;
        _organizationPermission = organizationPermission;
        _userAppService = userAppService;
        _identityUserManager = identityUserManager;
    }


    public async Task CreateAsync(Guid projectId, string keyName, Guid? currentUserId)
    {
        _logger.LogDebug($"[ProjectAppIdService][CreateAsync] projectId:{projectId}, keyName:{keyName}");

        if (await _projectAppIdRepository.CheckProjectAppNameExist(projectId, keyName))
        {
            throw new BusinessException(message: "key name has exist");
        }

        var appSecret = MD5Util.CalculateMD5($"{projectId.ToString()}-{keyName}-{Guid.NewGuid()}");
        var appId = Guid.NewGuid().ToString("N");
        var projectAppIdInfo = new ProjectAppIdInfo(Guid.NewGuid(), projectId, keyName, appId, appSecret)
        {
            CreationTime = DateTime.Now,
            CreatorId = currentUserId,
        };
        
        await _userAppService.RegisterClientAuthentication(appId, appSecret);
        await _projectAppIdRepository.InsertAsync(projectAppIdInfo);
    }

    public async Task DeleteAsync(Guid appId)
    {
        var appIdInfo = await _projectAppIdRepository.GetAsync(appId);
        if (appIdInfo == null)
        {
            throw new UserFriendlyException("Api key not found");
        }
        
        await _organizationPermission.AuthenticateAsync(appIdInfo.ProjectId, AevatarPermissions.ApiKeys.Delete);
        await _projectAppIdRepository.HardDeleteAsync(f => f.Id == appId);
        await _userAppService.DeleteClientAndAuthentication(appIdInfo.AppId);
        _logger.LogDebug($"[ProjectAppIdService][DeleteAsync] appId:{appId}");
    }

    public async Task ModifyApiKeyAsync(Guid appId, string keyName)
    {
        _logger.LogDebug($"[ProjectAppIdService][ModifyApiKeyAsync] appId:{appId}, keyName:{keyName}");

        var appIdInfo = await _projectAppIdRepository.GetAsync(appId);
        if (appIdInfo == null)
        {
            throw new BusinessException(message: "AppId not exist");
        }

        await _organizationPermission.AuthenticateAsync(appIdInfo.ProjectId, AevatarPermissions.ApiKeys.Edit);
        if (await _projectAppIdRepository.CheckProjectAppNameExist(appIdInfo.ProjectId, keyName))
        {
            throw new BusinessException(message: "key name has exist");
        }

        if (appIdInfo.AppName == keyName)
        {
            throw new BusinessException(message: "AppId is the same ");
        }


        appIdInfo.AppName = keyName;

        await _projectAppIdRepository.UpdateAsync(appIdInfo);
    }

    public async Task<List<ProjectAppIdListResponseDto>> GetApiKeysAsync(Guid projectId)
    {
        APIKeyPagedRequestDto requestDto = new APIKeyPagedRequestDto()
            { ProjectId = projectId, MaxResultCount = 10, SkipCount = 0 };

        var appIdList = await _projectAppIdRepository.GetProjectAppIds(requestDto);
        
        var result = new List<ProjectAppIdListResponseDto>();
        foreach (var item in appIdList.Items)
        {
            var creatorInfo = await _identityUserManager.GetByIdAsync((Guid)item.CreatorId!);
            result.Add(new ProjectAppIdListResponseDto()
            {
                Id = item.Id,
                AppId = item.AppId,
                AppSecret = item.AppSecret,
                AppName = item.AppName,
                CreateTime = item.CreationTime,
                CreatorName = creatorInfo.NormalizedUserName,
                ProjectId = item.ProjectId,
            });
        }

        return result;
    }
}