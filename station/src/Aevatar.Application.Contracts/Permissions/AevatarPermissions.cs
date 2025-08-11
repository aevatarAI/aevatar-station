namespace Aevatar.Permissions
{
    public static class AevatarPermissions
    {
        public const string BasicUser = "basicUser";
        public const string DeveloperManager = "developerManager";
        public const string AdminGroup = "AdminManagement";
        public const string AdminPolicy = AdminGroup+".AdminPolicy";
        public const string DeveloperPlatform = "DeveloperPlatform";
        
        public const string OrganizationScopeKey = "Scope";
        
        // Permissions for Agent Management
        public static class Agent
        {
            public const string GroupName = "AgentManagement";
            public const string ViewLogs = GroupName + ".ViewLogs";
            public const string ViewAllType = GroupName + ".ViewAllType";
            public const string ViewList = GroupName + ".ViewList";
            public const string Create = GroupName + ".Create";
            public const string View = GroupName + ".View";
            public const string Update = GroupName + ".Update";
            public const string Delete = GroupName + ".Delete";
        }

        // Permissions for Relationship Management
        public static class Relationship
        {
            public const string GroupName = "AgentRelationshipManagement";

            public const string ViewRelationship = GroupName + ".View";
            public const string AddSubAgent = GroupName + ".AddSubAgent";
            public const string RemoveSubAgent = GroupName + ".RemoveSubAgent";
            public const string RemoveAllSubAgents = GroupName + ".RemoveAllSubAgents";
        }

        // Permissions for Event Management
        public static class EventManagement
        {
            public const string GroupName = "EventManagement";

            public const string Publish = GroupName + ".Publish";
            public const string View = GroupName + ".View"; 
        }
        
        public static class HostManagement
        {
            public const string GroupName = "HostManagement"; 
            public const string Logs = GroupName + ".ViewLogs"; 
        }
        
        public static class CqrsManagement
        {
            public const string GroupName = "CqrsManagement"; 
            public const string Logs = GroupName + ".ViewLogs"; 
            public const string States = GroupName + ".ViewStates"; 
        }
        
        public static class SubscriptionManagent
        {
            public const string GroupName = "SubscriptionManagement"; 
            
            public const string CreateSubscription = GroupName + ".CreateSubscription"; 
            public const string CancelSubscription = GroupName + ".CancelSubscription"; 
            public const string ViewSubscriptionStatus = GroupName + ".ViewSubscription"; 
        }
        
        public static class Organizations
        {
            public const string Default = DeveloperPlatform + ".Organizations";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
        }
        
        public static class Members
        {
            public const string Default = DeveloperPlatform + ".Members";
            public const string Manage = Default + ".Manage";
        }
        
        public static class Projects
        {
            public const string Default = DeveloperPlatform + ".Projects";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
        }
        
        public static class ApiKeys
        {
            public const string Default = DeveloperPlatform + ".ApiKeys";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
        }
        
        public static class Roles
        {
            public const string Default = DeveloperPlatform + ".Roles";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
        }
        
        public const string Dashboard = DeveloperPlatform + ".Dashboards";
        
        public static class LLMSModels
        {
            public const string Default = Dashboard + ".LLMSModels";
        }
        
        public static class ApiRequests
        {
            public const string Default = Dashboard + ".ApiRequests";
        }
        
        public static class ProjectCorsOrigins
        {
            public const string Default = DeveloperPlatform + ".ProjectCorsOrigins";
            public const string Create = Default + ".Create";
            public const string Delete = Default + ".Delete";
        }
        
        public static class Plugins
        {
            public const string Default = DeveloperPlatform + ".Plugins";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
        }
    }
}
