using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Controllers;

[Route("api/permissions")]
public class PermissionManagementController : AbpController
{
    private readonly IPermissionManager _permissionManager;
    private readonly IIdentityRoleAppService _roleAppService;

    public PermissionManagementController(
        IPermissionManager permissionManager,
        IIdentityRoleAppService roleAppService)
    {
        _permissionManager = permissionManager;
        _roleAppService = roleAppService;
    }

    [HttpPost]
    public async Task<IActionResult> AssignPermissionToRole(string roleName, string permissionName)
    {
        // Assign a specific permission to a role
        await _permissionManager.SetForRoleAsync(roleName, permissionName, true);
        return Ok($"Permission '{permissionName}' assigned to role '{roleName}'");
    }
}