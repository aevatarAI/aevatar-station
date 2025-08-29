using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string AgentId { get; set; } = string.Empty;
    [Id(2)] public string AgentType { get; set; } = string.Empty; // Orchestrator, Specialized
    [Id(3)] public string Description { get; set; } = string.Empty;
    [Id(4)] public List<AgentExample> Examples { get; set; } = new();
    [Id(5)] public List<ToolDefinition> Tools { get; set; } = new();

    public bool Equals(AgentDescriptor? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name &&
               AgentId == other.AgentId &&
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
public class AgentWithUsage : AgentDescriptor
{
    [Id(1)] public string HandlingTask { get; set; } = string.Empty;
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
    [Id(0)] public string AgentName { get; set; } = string.Empty;
    [Id(1)] public string AgentId { get; set; } = string.Empty;
    [Id(2)] public string CallId { get; set; } = string.Empty;
    [Id(3)] public string Message { get; set; } = string.Empty;
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
    public string? Thought { get; set; }

    [Id(1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Response { get; set; }
}

[GenerateSerializer]
[Description("Holds the detailed PRD of the task")]
public class FramedTask
{
    [Id(0), Description("The title of the task in a few word.")]
    public string Title { get; set; } = string.Empty;

    [Id(1), Description("Understand user's intention in user's scenario.")]
    public string Intention { get; set; } = string.Empty;

    [Id(2), Description("A detailed description of the task.")]
    public string DetailedDescription { get; set; } = string.Empty;

    [Id(3), Description(
         @"List of multi-dimensional criteria for evaluating the quality of the result. It ensures the result is thorough,
         comprehensive and meets the user's expectations. It has to be useful for the user.
         "
         )]
    public string AcceptanceCriteria { get; set; } = string.Empty;
}

[GenerateSerializer]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoStatus
{
    Undefined,
    Pending,
    InProgress,
    Completed,
    Canceled
}

[GenerateSerializer]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoPriority
{
    Undefined,
    High,
    Medium,
    Low,
}

[GenerateSerializer]
public class TodoItem
{
    [Id(0), Required, Description("The id of the todo item. It's required.")]
    public string Id { get; set; } = string.Empty;

    [Id(1), Required, Description("The status of the todo item. It's required.")]
    public TodoStatus Status { get; set; } = TodoStatus.Undefined;

    [Id(2), Required, Description("The description of the todo task. It's required.")]
    public string Content { get; set; } = string.Empty;

    [Id(3), Required, Description("The priority of the todo item. It's required.")]
    public TodoPriority Priority { get; set; } = TodoPriority.Undefined;

    [Id(4), Required, Description("The list of Id's of other todo items this item depends on. It's required.")]
    public List<string> Dependencies { get; set; } = new();

    [Id(5), Description("The id of the agent this task is dispatched to.")]
    public string AssigneeAgentName { get; set; } = string.Empty;
}

[GenerateSerializer, Description("Contains all information the child agent to perform the task.")]
public class TaskDispatch
{
    [Id(1), Description("The description of the task to be performed.")]
    public string Task { get; set; } = string.Empty;

    [Id(2),
     Description(
         "Provide the background of the task explaining why we need to do it in the context of the parent task.")]
    public string Background { get; set; } = string.Empty;

    [Id(3), Description("Provides all known information that is needed to perform the task.")]
    public List<string> Knowledge { get; set; } = new();
}

[GenerateSerializer]
public class Artifact
{
    [Id(0),
     Description("The name of the artifact. It has to be unique and must be a valid file name with a valid extension.")]
    public string Name { get; set; } = string.Empty;

    [Id(1), Description("The format of the artifact. It has to be a valid file extension.")]
    public string Format { get; set; } = string.Empty;

    [Id(2), Description("The content of the artifact.")]
    public string Content { get; set; } = string.Empty;
}


[GenerateSerializer, JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewDecision
{
    UNDEFINED,
    APPROVED,
    NEEDS_FIXES,
    MAJOR_ISSUES
}

[GenerateSerializer]
public class ReviewResult
{
    [Id(0)] public ReviewDecision Decision { get; set; } = ReviewDecision.APPROVED;
    [Id(1)] public string Comment { get; set; } = string.Empty;
}

[GenerateSerializer]
public class FinalResponse
{
    [Id(0)] public string Response { get; set; } = string.Empty;
    [Id(1)] public List<Artifact> Artifacts { get; set; } = new();
}