// using System;
// using System.Threading.Tasks;
// using Aevatar.Service;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Moq;
// using OpenIddict.Abstractions;
// using Shouldly;
// using Volo.Abp;
// using Volo.Abp.Authorization.Permissions;
// using Volo.Abp.Identity;
// using Volo.Abp.PermissionManagement;
// using Volo.Abp.Users;
// using Xunit;
//
// namespace Aevatar.service
// {
//     public class UserAppServiceTests
//     {
//         private readonly Mock<IdentityUserManager> _userManager;
//         private readonly Mock<IIdentityUserRepository> _userRepository;
//         private readonly Mock<IIdentityRoleRepository> _roleRepository;
//         private readonly Mock<IOptions<IdentityOptions>> _identityOptions;
//         private readonly Mock<IOpenIddictApplicationManager> _applicationManager;
//         private readonly Mock<ILogger<UserAppService>> _logger;
//         private readonly Mock<IPermissionChecker> _permissionChecker;
//         private readonly Mock<IPermissionManager> _permissionManager;
//         private readonly Mock<ICurrentUser> _currentUser;
//         private readonly UserAppService _userAppService;
//
//     }
// } 