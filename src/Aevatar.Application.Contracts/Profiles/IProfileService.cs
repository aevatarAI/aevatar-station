using System;
using System.Threading.Tasks;

namespace Aevatar.Profile
{
    /// <summary>
    /// Profile service interface for user information
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Get user information by userId
        /// </summary>
        /// <param name="userId">User unique identifier</param>
        /// <returns>UserInfoDto</returns>
        Task<UserInfoDto> GetUserInfoAsync(Guid userId);
    }
} 