namespace GroupChat.GAgent.Feature.Common;

[GenerateSerializer]
public class ChatResponse
{
    [Id(0)] public bool Continue { get; set; } = true;
    [Id(1)] public bool Skip { get; set; } = false;
    [Id(2)] public string Content { get; set; }
}

