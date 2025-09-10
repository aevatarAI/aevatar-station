namespace Aevatar.GAgents.PsiOmni.Models;

[Serializable]
[GenerateSerializer]
public class ToolCall
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string FunctionName { get; set; } = string.Empty;
    [Id(2)] public string FunctionArguments { get; set; } = string.Empty;
}

[Serializable]
[GenerateSerializer]
public class SerializedChatMessageContent
{
    [Id(0)] public string TypeFullName { get; set; } = string.Empty;
    [Id(1)] public string Json { get; set; } = string.Empty;
}

[Serializable]
[GenerateSerializer]
public class TokenUsage
{
    [Id(0)] public int PromptTokens { get; set; } = 0;
    [Id(1)] public int CompletionTokens { get; set; } = 0;
    [Id(2)] public int TotalTokens { get; set; }
}

[Serializable]
[GenerateSerializer]
public class PsiOmniChatMessage
{
    [Id(0)] public string Role { get; set; } = string.Empty;

    [Id(1)] public string Content { get; set; } = string.Empty;

    [Id(2)] public string? Name { get; set; }

    [Id(3)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Id(4)] public Dictionary<string, object> Metadata { get; set; } = new();

    [Id(5)] public List<ToolCall> ToolCalls { get; set; } = new();
    [Id(6)] public SerializedChatMessageContent? Serialized { get; set; }
    [Id(7)] public TokenUsage? TokenUsage { get; set; } = null;

    /// <summary>
    /// Default constructor
    /// </summary>
    public PsiOmniChatMessage()
    {
    }

    /// <summary>
    /// Constructor with role and content
    /// </summary>
    public PsiOmniChatMessage(string role, string? content, string? name = null, List<ToolCall>? toolCalls = null)
    {
        Role = role ?? string.Empty;
        Content = content ?? string.Empty;
        Name = name ?? string.Empty;
        ToolCalls = toolCalls ?? new List<ToolCall>();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a system message
    /// </summary>
    public static PsiOmniChatMessage CreateSystemMessage(string content)
    {
        return new PsiOmniChatMessage("system", content);
    }

    /// <summary>
    /// Creates a user message
    /// </summary>
    public static PsiOmniChatMessage CreateUserMessage(string content)
    {
        return new PsiOmniChatMessage("user", content);
    }

    /// <summary>
    /// Creates an assistant message
    /// </summary>
    public static PsiOmniChatMessage CreateAssistantMessage(string content)
    {
        return new PsiOmniChatMessage("assistant", content);
    }

    public override string ToString()
    {
        return Content;
    }
}