1、我的设计
HTTP接口设计
GET /api/profile/user-info
Authorization: Bearer {jwt_token}
请求参数
- 无需参数，从JWT Token中获取当前用户ID
响应参数
{
  "uid": "3fa85f64-5717-4562-b3fc-2c963f66afa6", 
  "email": "user@example.com",
  "avatar": null, // 当前版本返回null，后续版本支持
  "name": "张三", // 来自GodGPT FullName，可选
  "userName": "user123" // 来自Identity用户名，备选显示
}

本系统需改造的
新增DTO类
文件路径: src/Aevatar.Application.Contracts/Profile/UserInfoDto.cs
using System;

namespace Aevatar.Profile;

public class UserInfoDto 
{
    /// <summary>
    /// 用户唯一标识
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// 用户邮箱
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// 用户头像URL，当前版本返回null
    /// </summary>
    public string? Avatar { get; set; }
    
    /// <summary>
    /// 用户姓名，来自GodGPT Profile的FullName
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// 用户名，作为姓名的备选显示
    /// </summary>
    public string? UserName { get; set; }
}
新增Service接口和实现
接口文件: src/Aevatar.Application.Contracts/Profile/IProfileService.cs
using System;
using System.Threading.Tasks;

namespace Aevatar.Profile;

public interface IProfileService
{
    Task<UserInfoDto> GetUserInfoAsync(Guid userId);
}
实现文件: src/Aevatar.Application/Service/ProfileService.cs
using System;
using System.Threading.Tasks;
using Aevatar.Profile;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;

namespace Aevatar.Service;

public class ProfileService : ApplicationService, IProfileService
{
    private readonly IdentityUserManager _userManager;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IdentityUserManager userManager,
        IClusterClient clusterClient,
        ILogger<ProfileService> logger)
    {
        _userManager = userManager;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<UserInfoDto> GetUserInfoAsync(Guid userId)
    {
        // 1. 获取Identity用户基础信息
        var identityUser = await _userManager.FindByIdAsync(userId.ToString());
        if (identityUser == null)
        {
            throw new Volo.Abp.UserFriendlyException("User not found");
        }

        // 2. 获取GodGPT Profile信息
        string? fullName = null;
        try
        {
            var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
            var userProfile = await manager.GetUserProfileAsync();
            fullName = !string.IsNullOrWhiteSpace(userProfile?.FullName) ? userProfile.FullName : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user profile from GodGPT for user {UserId}", userId);
        }

        // 3. 组装返回数据
        return new UserInfoDto
        {
            Uid = userId,
            Email = identityUser.Email,
            Avatar = null, // 当前版本不支持头像
            Name = fullName,
            UserName = identityUser.UserName
        };
    }
}

新增Controller
文件路径: src/Aevatar.HttpApi/Controllers/ProfileController.cs
using System;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Profile")]
[Route("api/profile")]
[Authorize]
public class ProfileController : AevatarController
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    [HttpGet("user-info")]
    public async Task<UserInfoDto> GetUserInfoAsync()
    {
        var userId = (Guid)CurrentUser.Id!;
        _logger.LogDebug("[ProfileController][GetUserInfoAsync] UserId: {UserId}", userId);
        
        return await _profileService.GetUserInfoAsync(userId);
    }
}
依赖注入配置
在 src/Aevatar.Application/AevatarApplicationModule.cs 中添加：
// 在Configure方法中添加
context.Services.AddTransient<IProfileService, ProfileService>();
实现步骤
1. 创建DTO类 - UserInfoDto
2. 创建Service接口和实现 - IProfileService, ProfileService  
3. 创建Controller - ProfileController
4. 配置依赖注入 - 在ApplicationModule中注册
5. 测试接口 - 使用Postman或Swagger测试

下游 GodGPT.GAgents 需要的支持
现有接口评估 
使用现有接口: IChatManagerGAgent.GetUserProfileAsync()
返回数据:
public class UserProfileDto 
{
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string BirthPlace { get; set; }
    public string FullName { get; set; } // 我们需要的name字段
}

结论: 无需修改GodGPT.GAgents，现有接口已满足需求。