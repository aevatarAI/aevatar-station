namespace Aevatar.PermissionManagement;

public interface IPermissionInfoProvider
{
    List<PermissionInfo> GetAllPermissionInfos();
}