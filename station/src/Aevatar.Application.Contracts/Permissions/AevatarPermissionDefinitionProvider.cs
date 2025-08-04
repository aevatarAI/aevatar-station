using System.Linq;
using Aevatar.Localization;
using Aevatar.Organizations;
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
        
        var organizationsPermission = developerPlatformGroup.AddPermission(AevatarPermissions.Organizations.Default, L("Permission:Organizations"));
        organizationsPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.Organization;
        organizationsPermission.AddChild(AevatarPermissions.Organizations.Edit, L("Permission:Organizations.Edit")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.Organization;
        
        var projectsPermission = developerPlatformGroup.AddPermission(AevatarPermissions.Projects.Default, L("Permission:Projects"));
        projectsPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        projectsPermission.AddChild(AevatarPermissions.Projects.Create, L("Permission:Projects.Create")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.Organization;
        projectsPermission.AddChild(AevatarPermissions.Projects.Edit, L("Permission:Projects.Edit")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        projectsPermission.AddChild(AevatarPermissions.Projects.Delete, L("Permission:Projects.Delete")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.Organization;
        
        var organizationMembersPermission = developerPlatformGroup.AddPermission(AevatarPermissions.Members.Default, L("Permission:Members"));
        organizationMembersPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        organizationMembersPermission.AddChild(AevatarPermissions.Members.Manage, L("Permission:Members.Manage")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        
        var apiKeysPermission = developerPlatformGroup.AddPermission(AevatarPermissions.ApiKeys.Default, L("Permission:ApiKeys"));
        apiKeysPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        apiKeysPermission.AddChild(AevatarPermissions.ApiKeys.Create, L("Permission:ApiKeys.Create")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        apiKeysPermission.AddChild(AevatarPermissions.ApiKeys.Edit, L("Permission:ApiKeys.Edit")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        apiKeysPermission.AddChild(AevatarPermissions.ApiKeys.Delete, L("Permission:ApiKeys.Delete")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        
        var rolesPermission = developerPlatformGroup.AddPermission(AevatarPermissions.Roles.Default, L("Permission:Roles"));
        apiKeysPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        rolesPermission.AddChild(AevatarPermissions.Roles.Create, L("Permission:Roles.Create")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        rolesPermission.AddChild(AevatarPermissions.Roles.Edit, L("Permission:Roles.Edit")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        rolesPermission.AddChild(AevatarPermissions.Roles.Delete, L("Permission:Roles.Delete")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        
        var dashboardsPermission = developerPlatformGroup.AddPermission(AevatarPermissions.Dashboard, L("Permission:Dashboards"));
        dashboardsPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        dashboardsPermission.AddChild(AevatarPermissions.LLMSModels.Default, L("Permission:LLMSModels")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        dashboardsPermission.AddChild(AevatarPermissions.ApiRequests.Default, L("Permission:ApiRequests")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        
        var projectCorsOriginsPermission = developerPlatformGroup.AddPermission(AevatarPermissions.ProjectCorsOrigins.Default, L("Permission:ProjectCorsOrigins"));
        projectCorsOriginsPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        projectCorsOriginsPermission.AddChild(AevatarPermissions.ProjectCorsOrigins.Create, L("Permission:ProjectCorsOrigins.Create")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        projectCorsOriginsPermission.AddChild(AevatarPermissions.ProjectCorsOrigins.Delete, L("Permission:ProjectCorsOrigins.Delete")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        
        var pluginsPermission = developerPlatformGroup.AddPermission(AevatarPermissions.Plugins.Default, L("Permission:Plugins"));
        pluginsPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        pluginsPermission.AddChild(AevatarPermissions.Plugins.Create, L("Permission:Plugins.Create")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        pluginsPermission.AddChild(AevatarPermissions.Plugins.Edit, L("Permission:Plugins.Edit")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        pluginsPermission.AddChild(AevatarPermissions.Plugins.Delete, L("Permission:Plugins.Delete")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        
        var mcpServersPermission = developerPlatformGroup.AddPermission(AevatarPermissions.McpServers.Default, L("Permission:McpServers"));
        mcpServersPermission.Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        mcpServersPermission.AddChild(AevatarPermissions.McpServers.Create, L("Permission:McpServers.Create")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        mcpServersPermission.AddChild(AevatarPermissions.McpServers.Edit, L("Permission:McpServers.Edit")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
        mcpServersPermission.AddChild(AevatarPermissions.McpServers.Delete, L("Permission:McpServers.Delete")).Properties[AevatarPermissions.OrganizationScopeKey] = PermissionScope.OrganizationAndProject;
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AevatarResource>(name);
    }
}