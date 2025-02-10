namespace Aevatar.AI.Common;

public class ChatMessage
{
    public ChatRole ChatRole { get; set; }
    public string? Content { get; set; }
}

public enum ChatRole
{
    User,
    Assistant,
    System,
    // Function
}
