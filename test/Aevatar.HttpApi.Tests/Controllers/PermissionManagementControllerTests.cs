// using System;
// using System.Threading.Tasks;
// using Aevatar.Controllers;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.DependencyInjection;
// using Volo.Abp.Identity;
// using Volo.Abp.PermissionManagement;
// using Volo.Abp.Testing;
// using Xunit;
//
// namespace Aevatar.HttpApi.Tests.Controllers;
//
// public class PermissionManagementControllerTests : AbpIntegratedTest<AevatarHttpApiTestModule>
// {
//     private readonly IPermissionManager _permissionManager;
//     private readonly IIdentityRoleAppService _roleAppService;
//     private readonly PermissionManagementController _controller;
//
//     public PermissionManagementControllerTests()
//     {
//         _permissionManager = GetRequiredService<IPermissionManager>();
//         _roleAppService = GetRequiredService<IIdentityRoleAppService>();
//         _controller = new PermissionManagementController(_permissionManager, _roleAppService);
//     }
//
//     [Fact]
//     public async Task AssignPermissionToRole_ShouldSetPermission_AndReturnOk()
//     {
//         // Arrange
//         var roleName = "admin";
//         var permissionName = "MyApp.MyPermission";
//
//         // Act
//         var result = await _controller.AssignPermissionToRole(roleName, permissionName);
//
//         // Assert
//         var okResult = Assert.IsType<OkObjectResult>(result);
//         var message = Assert.IsType<string>(okResult.Value);
//         Assert.Contains(roleName, message);
//         Assert.Contains(permissionName, message);
//
//         // Verify the permission is set
//         var permission = await _permissionManager.GetForRoleAsync(roleName, permissionName);
//         Assert.True(permission.IsGranted);
//     }
//
//     [Theory]
//     [InlineData(null, "MyApp.MyPermission")]
//     [InlineData("admin", null)]
//     [InlineData("", "MyApp.MyPermission")]
//     [InlineData("admin", "")]
//     public async Task AssignPermissionToRole_WithInvalidInput_ShouldThrowException(string roleName, string permissionName)
//     {
//         // Act & Assert
//         await Assert.ThrowsAsync<ArgumentException>(
//             () => _controller.AssignPermissionToRole(roleName, permissionName));
//     }
//
//     // [Fact]
//     // public async Task AssignPermissionToRole_WithNonExistentRole_ShouldThrowException()
//     // {
//     //     // Arrange
//     //     var roleName = "non-existent-role";
//     //     var permissionName = "MyApp.MyPermission";
//     //
//     //     // Act & Assert
//     //     await Assert.ThrowsAsync<AbpPermissionManagementException>(
//     //         () => _controller.AssignPermissionToRole(roleName, permissionName));
//     // }
// }