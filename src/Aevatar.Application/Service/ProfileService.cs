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

            // _logger.LogWarning("GetUserInfoAsync:+{A}",JsonConvert.SerializeObject(identityUser));

            // Check for Apple private relay email (privacy protection)
            if (IsApplePrivateRelay(identityUser.UserName))
            {
                // 2. Profile info for private relay users
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

                return new UserInfoDto
                {
                    Uid = userId,
                    Email = null, // Privacy protection - no email for private relay
                    Avatar = null, // not support user logo
                    Name = fullName
                };
            }

            // Extract email based on login type
            string email = ExtractRealEmail(identityUser.UserName, identityUser.Email);

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

            // Extract display name
            string displayName = ExtractDisplayName(identityUser.UserName, email, fullName);

            // 3. 
            return new UserInfoDto
            {
                Uid = userId,
                Email = email,
                Avatar = null, // not support user logo
                Name = displayName
            };
        }

        /// <summary>
        /// Extract real email address based on login type
        /// </summary>
        private string ExtractRealEmail(string userName, string systemEmail)
        {
            // Check if it's third-party login (Apple/Google)
            if (IsThirdPartyLogin(userName))
            {
                return ExtractEmailFromThirdPartyUserName(userName);
            }
            
            // For regular email login, use system email
            return systemEmail;
        }

        /// <summary>
        /// Extract display name based on available information
        /// </summary>
        private string ExtractDisplayName(string userName, string email, string? fullName)
        {
            // Priority 1: Use fullName from GodGPT profile if available
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            // Priority 2: For third-party login, extract from email
            if (IsThirdPartyLogin(userName))
            {
                return GetDisplayNameFromEmail(email);
            }

            // Priority 3: For regular login with custom username (not GUID), use username
            if (!IsGuidFormat(userName))
            {
                return userName;
            }

            // Priority 4: Extract from email as fallback
            return GetDisplayNameFromEmail(email);
        }

        /// <summary>
        /// Check if the login is from third-party providers
        /// </summary>
        private bool IsThirdPartyLogin(string userName)
        {
            return userName?.EndsWith("@apple", StringComparison.OrdinalIgnoreCase) == true ||
                   userName?.EndsWith("@google", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Extract email from third-party login username
        /// </summary>
        private string ExtractEmailFromThirdPartyUserName(string userName)
        {
            if (userName.EndsWith("@apple", StringComparison.OrdinalIgnoreCase))
            {
                return userName.Substring(0, userName.Length - "@apple".Length);
            }
            if (userName.EndsWith("@google", StringComparison.OrdinalIgnoreCase))
            {
                return userName.Substring(0, userName.Length - "@google".Length);
            }
            return userName;
        }

        /// <summary>
        /// Check if the text is in GUID format
        /// </summary>
        private bool IsGuidFormat(string text)
        {
            return Guid.TryParse(text, out _);
        }

        /// <summary>
        /// Extract display name from email address
        /// </summary>
        private string GetDisplayNameFromEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return email ?? "User";
            }

            // Extract the part before @ symbol
            var emailPrefix = email.Substring(0, email.IndexOf("@"));
            
            // Remove common number suffixes and make it more readable
            return emailPrefix;
        }

        /// <summary>
        /// Check if the username is Apple's private relay format
        /// </summary>
        private bool IsApplePrivateRelay(string userName)
        {
            return userName?.EndsWith("@apple.privaterelay.com@apple", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
} 