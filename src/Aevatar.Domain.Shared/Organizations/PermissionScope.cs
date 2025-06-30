using System;

namespace Aevatar.Organizations;

[Flags]
public enum PermissionScope : byte
{
    Organization = 1,

    Project = 2,

    OrganizationAndProject = Organization | Project
}
