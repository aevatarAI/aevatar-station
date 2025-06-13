using System;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Profiles;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;

namespace Aevatar.Service
{
    /// <summary>
    /// Profile service implementation for user information
    /// </summary>
    public class ProfileService : ApplicationService, IProfileService
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<ProfileService> _logger;
        private readonly IdentityUserManager _identityUserManager;

        public ProfileService(
            IClusterClient clusterClient,
            ILogger<ProfileService> logger,
            IdentityUserManager identityUserManager)
        {
            _clusterClient = clusterClient;
            _logger = logger;
            _identityUserManager = identityUserManager;
        }

        public async Task<UserInfoDto> GetUserInfoAsync(Guid userId)
        {
            // 1. 获取Identity用户基础信息
            var identityUser = await _identityUserManager.FindByIdAsync(userId.ToString());
            if (identityUser == null)
            {
                throw new Volo.Abp.UserFriendlyException("User not found");
            }
            string email = identityUser.Email;
            string userName = identityUser.UserName;

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
                Email = email,
                Avatar = null, // 当前版本不支持头像
                Name = fullName,
                UserName = userName
            };
        }
    }
} 