using System;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Profiles;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            // 1. basic info
            var identityUser = await _identityUserManager.FindByIdAsync(userId.ToString());
            if (identityUser == null)
            {
                _logger.LogError("User not found,return default info,Failed to get user profile from GodGPT for user {UserId}", userId);
                return new UserInfoDto
                {
                    Uid = userId
                };
            }

            _logger.LogWarning("GetUserInfoAsync:+{A}",JsonConvert.SerializeObject(identityUser));
            string email = identityUser.Email;
            //string userName = identityUser.UserName;

            // 2. Profile info
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

            // 3. 
            return new UserInfoDto
            {
                Uid = userId,
                Email = email,
                Avatar = null, // not support user logo
                Name = fullName
            };
        }
    }
} 