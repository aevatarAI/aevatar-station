namespace Aevatar.Core.Abstractions.Plugin;

/// <summary>
/// Marks a class as an agent plugin implementation
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AgentPluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string? Description { get; set; }

    public AgentPluginAttribute(string name, string version)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }
}

/// <summary>
/// Marks a method as callable from Orleans grains
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AgentMethodAttribute : Attribute
{
    public string? MethodName { get; set; }
    public bool IsReadOnly { get; set; }
    public bool AlwaysInterleave { get; set; }
    public bool OneWay { get; set; }

    public AgentMethodAttribute(string? methodName = null)
    {
        MethodName = methodName;
    }
}

/// <summary>
/// Marks a method as an event handler
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AgentEventHandlerAttribute : Attribute
{
    public string? EventType { get; set; }
    public bool AllowSelfHandling { get; set; }

    public AgentEventHandlerAttribute(string? eventType = null)
    {
        EventType = eventType;
    }
}

/// <summary>
/// Marks a method as a state handler
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AgentStateHandlerAttribute : Attribute
{
    public string? StateType { get; set; }

    public AgentStateHandlerAttribute(string? stateType = null)
    {
        StateType = stateType;
    }
}

/// <summary>
/// Configures agent initialization parameters
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AgentConfigurationAttribute : Attribute
{
    public string? StateProvider { get; set; }
    public string? LogProvider { get; set; }
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
}

/// <summary>
/// Marks a property as injectable from the agent context
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class AgentInjectAttribute : Attribute
{
    public string? ServiceName { get; set; }
    public bool Required { get; set; } = true;

    public AgentInjectAttribute(string? serviceName = null)
    {
        ServiceName = serviceName;
    }
}