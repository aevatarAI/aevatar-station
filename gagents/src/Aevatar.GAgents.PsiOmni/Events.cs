using System.Text.Json.Serialization;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.PsiOmni.Models;

namespace Aevatar.GAgents.PsiOmni;

[GenerateSerializer]
public class AgentConfigEvent : EventBase
{
    [Id(0)] public string UniqueId { get; } = Guid.NewGuid().ToString();
    [Id(1)] public AgentConfiguration Configuration { get; set; } = new();
    [Id(2)] public string ParentAgentId { get; set; } = string.Empty;

    [Id(3)]
    public List<string> Tools { get; set; } = new(); //TODO: Kept here to cater to old code. Need to be delelted.
}

/// <summary>
/// User agent sends to target agent
/// </summary>
[GenerateSerializer]
public class UserMessageEvent : EventBase
{
    [Id(0)] public string UniqueId { get; } = Guid.NewGuid().ToString();
    [Id(1)] public string TargetAgentId { get; set; } = string.Empty;
    [Id(2)] public string CallId { get; set; } = string.Empty;
    [Id(3)] public string Content { get; set; } = string.Empty;
    [Id(4)] public string? ReplyToAgentId { get; set; }

    public override string ToString()
    {
        return Content;
    }
}

/// <summary>
/// Agent's reply message
/// </summary>
[GenerateSerializer]
public class AgentMessageEvent : EventBase
{
    [Id(0)] public string UniqueId { get; } = Guid.NewGuid().ToString();
    [Id(1)] public string TargetAgentId { get; set; } = string.Empty;
    [Id(2)] public string CallId { get; set; } = string.Empty;
    [Id(3)] public string Content { get; set; } = string.Empty;
    [Id(4)] public string SenderAgentId { get; set; } = string.Empty;
    [Id(5)] public string SenderAgentName { get; set; } = string.Empty;
    [Id(6)] public List<Artifact> Artifacts { get; set; } = new();

    public override string ToString()
    {
        return Content;
    }
}



[GenerateSerializer]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContinuationType
{
    Unspecified,
    Initialize,
    Run,
    SelfReportAndRun,
    RegisterAgents,
    Retrospect,
    IterateOrSelfReportAndReply,
    SelfReport
}

/// <summary>
/// Send to self to continue processing
/// </summary>
[GenerateSerializer]
public class ContinuationEvent : EventBase
{
    [Id(0)] public string UniqueId { get; } = Guid.NewGuid().ToString();
    [Id(1)] public string TargetAgentId { get; set; } = string.Empty;
    [Id(2)] public ContinuationType ContinuationType { get; set; }
    [Id(3)] public string RunArg { get; set; } = string.Empty;
    [Id(4)] public List<string> RegisterAgentIds { get; set; } = new();
    [Id(5)] public FinalResponse FinalResponse { get; set; } = new();
}

[GenerateSerializer]
public class SelfReportEvent : EventBase
{
    [Id(0)] public string UniqueId { get; } = Guid.NewGuid().ToString();
    [Id(1)] public string TargetAgentId { get; set; } = string.Empty;
    [Id(2)] public AgentDescriptor SelfReport { get; set; } = new();
}