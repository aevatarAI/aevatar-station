using System.Text.Json.Serialization;

namespace Aevatar.GAgents.PsiOmni;

[Serializable]
[GenerateSerializer]
public class AgentExample : IEquatable<AgentExample>
{
    [Id(0)] public string Request { get; set; } = string.Empty;

    [Id(1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Response { get; set; }

    public bool Equals(AgentExample? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Request == other.Request && Response == other.Response;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AgentExample)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Request, Response);
    }
}

[Serializable]
[GenerateSerializer]
public class AgentDescriptor : IEquatable<AgentDescriptor>
{
    [Id(0)] public string AgentId { get; set; } = string.Empty;
    [Id(1)] public string AgentType { get; set; } = string.Empty; // Orchestrator, Specialized
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public List<AgentExample> Examples { get; set; } = new();
    [Id(4)] public List<ToolDefinition> Tools { get; set; } = new();

    public bool Equals(AgentDescriptor? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return AgentId == other.AgentId &&
               AgentType == other.AgentType &&
               Description == other.Description &&
               Examples.SequenceEqual(other.Examples) &&
               Tools.SequenceEqual(other.Tools);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AgentDescriptor)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(AgentId);
        hashCode.Add(AgentType);
        hashCode.Add(Description);
        Examples.ForEach(e => hashCode.Add(e));
        Tools.ForEach(t => hashCode.Add(t));
        return hashCode.ToHashCode();
    }
}

[GenerateSerializer]
public class RealizationResult
{
    [Id(0)] public string OperationMode { get; set; } = "UNKNOWN";
    [Id(1)] public string Description { get; set; } = string.Empty;
    [Id(2)] public List<string> Tools { get; set; } = new();
}

[GenerateSerializer]
public class AgentCall
{
    [Id(0)] public string AgentId { get; set; } = string.Empty;
    [Id(1)] public string CallId { get; set; } = string.Empty;
    [Id(2)] public string Message { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ToolDefinition
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string Description { get; set; } = string.Empty;
    [Id(2)] public List<ToolParameter> Parameters { get; set; } = new();
}

[GenerateSerializer]
public class ToolParameter
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string Description { get; set; } = string.Empty;
    [Id(2)] public bool IsRequired { get; set; } = true;
    [Id(3)] public string Schema { get; set; } = string.Empty;
}

[GenerateSerializer]
public class OrchestratorMessage
{
    [Id(0), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Intermediate { get; set; }

    [Id(1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Final { get; set; }
}