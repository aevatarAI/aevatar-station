using System;

namespace Aevatar.Profiles
{
    /// <summary>
    /// User information Data Transfer Object
    /// </summary>
    public class UserInfoDto
    {
        /// <summary>
        /// User unique identifier
        /// </summary>
        public Guid Uid { get; set; }

        /// <summary>
        /// User email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// User avatar URL, currently returns null
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// User full name from GodGPT Profile
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// UserName as an alternative display
        /// </summary>
        public string? UserName { get; set; }
    }
} 