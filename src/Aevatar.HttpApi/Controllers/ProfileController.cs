using System;
using System.Threading.Tasks;
using Aevatar.Profile;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Controllers
{
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
} 