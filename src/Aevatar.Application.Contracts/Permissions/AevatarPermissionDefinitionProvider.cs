using System.Linq;
using Aevatar.Localization;
using Aevatar.PermissionManagement;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Aevatar.Permissions;

public class AevatarPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var adminManagement = context.AddGroup(AevatarPermissions.AdminGroup, L("Permission:AdminManagement"));
        adminManagement.AddPermission(AevatarPermissions.AdminPolicy, L("Permission:AdminPolicy"));

        // Agent Management
        var agentManagementGroup =
            context.AddGroup(AevatarPermissions.Agent.GroupName, L("Permission:AgentManagement"));
        agentManagementGroup.AddPermission(AevatarPermissions.Agent.ViewLogs, L("Permission:ViewLogs"));
        agentManagementGroup.AddPermission(AevatarPermissions.Agent.ViewAllType, L("Permission:ViewAll"));
        agentManagementGroup.AddPermission(AevatarPermissions.Agent.ViewList, L("Permission:ViewList"));
        agentManagementGroup.AddPermission(AevatarPermissions.Agent.Create, L("Permission:Create"));
        agentManagementGroup.AddPermission(AevatarPermissions.Agent.View, L("Permission:View"));
        agentManagementGroup.AddPermission(AevatarPermissions.Agent.Update, L("Permission:Update"));
        agentManagementGroup.AddPermission(AevatarPermissions.Agent.Delete, L("Permission:Delete"));

        // Relationship Management
        var relationshipGroup = context.AddGroup(AevatarPermissions.Relationship.GroupName,
            L("Permission:AgentRelationshipManagement"));
        relationshipGroup.AddPermission(AevatarPermissions.Relationship.ViewRelationship,
            L("Permission:ViewRelationship"));
        relationshipGroup.AddPermission(AevatarPermissions.Relationship.AddSubAgent, L("Permission:AddSubAgent"));
        relationshipGroup.AddPermission(AevatarPermissions.Relationship.RemoveSubAgent, L("Permission:RemoveSubAgent"));
        relationshipGroup.AddPermission(AevatarPermissions.Relationship.RemoveAllSubAgents,
            L("Permission:RemoveAllSubAgents"));

        // Event Management
        var eventManagementGroup =
            context.AddGroup(AevatarPermissions.EventManagement.GroupName, L("Permission:EventManagement"));
        eventManagementGroup.AddPermission(AevatarPermissions.EventManagement.Publish, L("Permission:PublishEvent"));
        eventManagementGroup.AddPermission(AevatarPermissions.EventManagement.View, L("Permission:ViewEvents"));


        // Host Management
        var hostManagementGroup =
            context.AddGroup(AevatarPermissions.HostManagement.GroupName, L("Permission:HostManagement"));
        hostManagementGroup.AddPermission(AevatarPermissions.HostManagement.Logs, L("Permission:ViewHostLogs"));

        // Cqrs Management
        var cqrsManagementGroup =
            context.AddGroup(AevatarPermissions.CqrsManagement.GroupName, L("Permission:CqrsManagement"));

        cqrsManagementGroup.AddPermission(AevatarPermissions.CqrsManagement.Logs, L("Permission:ViewLogs"));
        cqrsManagementGroup.AddPermission(AevatarPermissions.CqrsManagement.States, L("Permission:ViewStates"));

        // Subscription Management
        var subscriptionManagementGroup = context.AddGroup(AevatarPermissions.SubscriptionManagent.GroupName,
            L("Permission:SubscriptionManagement"));

        subscriptionManagementGroup.AddPermission(AevatarPermissions.SubscriptionManagent.CreateSubscription,
            L("Permission:CreateSubscription"));
        subscriptionManagementGroup.AddPermission(AevatarPermissions.SubscriptionManagent.CancelSubscription,
            L("Permission:CancelSubscription"));
        subscriptionManagementGroup.AddPermission(AevatarPermissions.SubscriptionManagent.ViewSubscriptionStatus,
            L("Permission:ViewSubscription"));
        var permissionInfos = GAgentPermissionHelper.GetAllPermissionInfos();
        var group = context.AddGroup("GAgents");
        foreach (var permissionInfo in permissionInfos)

        {
            if (context.GetPermissionOrNull(permissionInfo.Name) == null)
            {
                group.AddPermission(permissionInfo.Name, L(permissionInfo.Name));
            }
        }

        var developerPlatformGroup = context.AddGroup(AevatarPermissions.DeveloperPlatform);

        var organizationsPermission =
            developerPlatformGroup.AddPermission(AevatarPermissions.Organizations.Default,
                L("Permission:Organizations"));
        organizationsPermission.AddChild(AevatarPermissions.Organizations.Create, L("Permission:Organizations.Create"));
        organizationsPermission.AddChild(AevatarPermissions.Organizations.Edit, L("Permission:Organizations.Edit"));
        organizationsPermission.AddChild(AevatarPermissions.Organizations.Delete, L("Permission:Organizations.Delete"));

        var organizationMembersPermission =
            developerPlatformGroup.AddPermission(AevatarPermissions.OrganizationMembers.Default,
                L("Permission:OrganizationMembers"));
        organizationMembersPermission.AddChild(AevatarPermissions.OrganizationMembers.Manage,
            L("Permission:OrganizationMembers.Manage"));

        var apiKeysPermission =
            developerPlatformGroup.AddPermission(AevatarPermissions.ApiKeys.Default, L("Permission:ApiKeys"));
        apiKeysPermission.AddChild(AevatarPermissions.ApiKeys.Create, L("Permission:ApiKeys.Create"));
        apiKeysPermission.AddChild(AevatarPermissions.ApiKeys.Edit, L("Permission:ApiKeys.Edit"));
        apiKeysPermission.AddChild(AevatarPermissions.ApiKeys.Delete, L("Permission:ApiKeys.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AevatarResource>(name);
    }
}