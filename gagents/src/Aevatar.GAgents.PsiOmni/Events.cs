using Aevatar.Core.Abstractions;
using Aevatar.GAgents.PsiOmni.Models;

namespace Aevatar.GAgents.PsiOmni;

[GenerateSerializer]
public class AgentConfigEvent : EventBase
{
    [Id(0)] public AgentConfiguration Configuration { get; set; } = new();
    [Id(1)] public string ParentAgentId { get; set; } = string.Empty;

    [Id(2)]
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
    [Id(1)] public string TargetAgentId { get; set; }
    [Id(2)] public string CallId { get; set; }
    [Id(3)] public string Content { get; set; }

    public override string ToString()
    {
        return Content;
    }
}

[GenerateSerializer]
public class SelfReportEvent : EventBase
{
    [Id(0)] public string UniqueId { get; } = Guid.NewGuid().ToString();
    [Id(1)] public string TargetAgentId { get; set; } = string.Empty;
    [Id(2)] public AgentDescriptor SelfReport { get; set; } = new();
}