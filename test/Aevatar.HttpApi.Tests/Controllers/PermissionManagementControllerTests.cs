// using Aevatar.Controllers;
// using Microsoft.AspNetCore.Mvc;
// using Volo.Abp.Identity;
// using Volo.Abp.PermissionManagement;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace Aevatar.HttpApi.Tests.Controllers;
//
// public class PermissionManagementControllerTests : AevatarTestBase<AevatarHttpApiTestModule>
// {
//     private readonly IPermissionManager _permissionManager;
//     private readonly IIdentityRoleAppService _roleAppService;
//     private readonly PermissionManagementController _controller;
//     private readonly ITestOutputHelper _output;
//
//     public PermissionManagementControllerTests(ITestOutputHelper output)
//     {
//         _output = output;
//         _permissionManager = GetRequiredService<IPermissionManager>();
//         _roleAppService = GetRequiredService<IIdentityRoleAppService>();
//         _controller = new PermissionManagementController(_permissionManager, _roleAppService);
//     }
//
//     [Fact]
//     public async Task AssignPermissionToRole_ShouldAssignPermission()
//     {
//         await WithUnitOfWorkAsync(async () =>
//         {
//             // Arrange
//             var roleName = "TestRole";
//             var permissionName = "TestPermission";
//
//             // Act
//             var result = await _controller.AssignPermissionToRole(roleName, permissionName);
//
//             // Assert
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal($"Permission '{permissionName}' assigned to role '{roleName}'", okResult.Value);
//
//             // Verify the permission was actually set
//             var permissionResult = await _permissionManager.GetAsync(permissionName, "R", roleName);
//             Assert.NotNull(permissionResult);
//             Assert.True(permissionResult.IsGranted);
//         });
//     }
//
//     [Fact]
//     public async Task RevokePermissionFromRole_ShouldRevokePermission()
//     {
//         await WithUnitOfWorkAsync(async () =>
//         {
//             // Arrange
//             var roleName = "TestRole";
//             var permissionName = "TestPermission";
//             await _controller.AssignPermissionToRole(roleName, permissionName);
//
//             // Act
//             var result = await _controller.AssignPermissionToRole(roleName, permissionName);
//
//             // Assert
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal($"Permission '{permissionName}' revoked from role '{roleName}'", okResult.Value);
//
//             // Verify the permission was actually revoked
//             var permissionResult = await _permissionManager.GetAsync(permissionName, "R", roleName);
//             Assert.NotNull(permissionResult);
//             Assert.False(permissionResult.IsGranted);
//         });
//     }
// }