namespace Aevatar.Permissions;

/// <summary>
/// Permission constants for MCP Gateway operations
/// </summary>
public static class MCPGatewayPermissions
{
    /// <summary>
    /// Group name for MCP Gateway permissions
    /// </summary>
    public const string GroupName = "MCPGateway";

    /// <summary>
    /// Permissions for MCP Adapter management
    /// </summary>
    public static class Adapters
    {
        /// <summary>
        /// Base permission for adapter operations
        /// </summary>
        public const string Default = GroupName + ".Adapters";

        /// <summary>
        /// Permission to create new adapters
        /// </summary>
        public const string Create = Default + ".Create";

        /// <summary>
        /// Permission to view adapter information
        /// </summary>
        public const string Read = Default + ".Read";

        /// <summary>
        /// Permission to update existing adapters
        /// </summary>
        public const string Update = Default + ".Update";

        /// <summary>
        /// Permission to delete adapters
        /// </summary>
        public const string Delete = Default + ".Delete";

        /// <summary>
        /// Permission to manage all adapters (super admin)
        /// </summary>
        public const string ManageAll = Default + ".ManageAll";

        /// <summary>
        /// Permission to view adapter metrics and statistics
        /// </summary>
        public const string ViewMetrics = Default + ".ViewMetrics";

        /// <summary>
        /// Permission to test adapter connections
        /// </summary>
        public const string TestConnection = Default + ".TestConnection";

        /// <summary>
        /// Permission to view adapter logs
        /// </summary>
        public const string ViewLogs = Default + ".ViewLogs";
    }

    /// <summary>
    /// Permissions for MCP Gateway system operations
    /// </summary>
    public static class Gateway
    {
        /// <summary>
        /// Base permission for gateway operations
        /// </summary>
        public const string Default = GroupName + ".Gateway";

        /// <summary>
        /// Permission to view gateway health and status
        /// </summary>
        public const string ViewHealth = Default + ".ViewHealth";

        /// <summary>
        /// Permission to view gateway configuration
        /// </summary>
        public const string ViewConfiguration = Default + ".ViewConfiguration";

        /// <summary>
        /// Permission to update gateway configuration
        /// </summary>
        public const string UpdateConfiguration = Default + ".UpdateConfiguration";

        /// <summary>
        /// Permission to view system metrics
        /// </summary>
        public const string ViewSystemMetrics = Default + ".ViewSystemMetrics";

        /// <summary>
        /// Permission to manage gateway (restart, shutdown, etc.)
        /// </summary>
        public const string Manage = Default + ".Manage";

        /// <summary>
        /// Permission to view audit logs
        /// </summary>
        public const string ViewAuditLogs = Default + ".ViewAuditLogs";
    }

    /// <summary>
    /// Permissions for MCP sessions and connections
    /// </summary>
    public static class Sessions
    {
        /// <summary>
        /// Base permission for session operations
        /// </summary>
        public const string Default = GroupName + ".Sessions";

        /// <summary>
        /// Permission to view active sessions
        /// </summary>
        public const string View = Default + ".View";

        /// <summary>
        /// Permission to terminate sessions
        /// </summary>
        public const string Terminate = Default + ".Terminate";

        /// <summary>
        /// Permission to view session details and history
        /// </summary>
        public const string ViewDetails = Default + ".ViewDetails";

        /// <summary>
        /// Permission to manage session routing
        /// </summary>
        public const string ManageRouting = Default + ".ManageRouting";
    }

    /// <summary>
    /// Get all permission names
    /// </summary>
    public static string[] GetAll()
    {
        return new[]
        {
            // Adapter permissions
            Adapters.Default,
            Adapters.Create,
            Adapters.Read,
            Adapters.Update,
            Adapters.Delete,
            Adapters.ManageAll,
            Adapters.ViewMetrics,
            Adapters.TestConnection,
            Adapters.ViewLogs,

            // Gateway permissions
            Gateway.Default,
            Gateway.ViewHealth,
            Gateway.ViewConfiguration,
            Gateway.UpdateConfiguration,
            Gateway.ViewSystemMetrics,
            Gateway.Manage,
            Gateway.ViewAuditLogs,

            // Session permissions
            Sessions.Default,
            Sessions.View,
            Sessions.Terminate,
            Sessions.ViewDetails,
            Sessions.ManageRouting
        };
    }

    /// <summary>
    /// Get default permissions for different roles
    /// </summary>
    public static class DefaultRolePermissions
    {
        /// <summary>
        /// Permissions for MCP Gateway Administrator
        /// </summary>
        public static string[] Administrator => GetAll();

        /// <summary>
        /// Permissions for MCP Gateway Operator
        /// </summary>
        public static string[] Operator => new[]
        {
            Adapters.Read,
            Adapters.Create,
            Adapters.Update,
            Adapters.ViewMetrics,
            Adapters.TestConnection,
            Gateway.ViewHealth,
            Gateway.ViewConfiguration,
            Gateway.ViewSystemMetrics,
            Sessions.View,
            Sessions.ViewDetails
        };

        /// <summary>
        /// Permissions for MCP Gateway Viewer (read-only)
        /// </summary>
        public static string[] Viewer => new[]
        {
            Adapters.Read,
            Adapters.ViewMetrics,
            Gateway.ViewHealth,
            Gateway.ViewConfiguration,
            Gateway.ViewSystemMetrics,
            Sessions.View,
            Sessions.ViewDetails
        };

        /// <summary>
        /// Permissions for MCP Gateway Developer
        /// </summary>
        public static string[] Developer => new[]
        {
            Adapters.Read,
            Adapters.Create,
            Adapters.Update,
            Adapters.ViewMetrics,
            Adapters.TestConnection,
            Adapters.ViewLogs,
            Gateway.ViewHealth,
            Gateway.ViewSystemMetrics,
            Sessions.View
        };
    }
}
