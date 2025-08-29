using Aevatar.GAgents.AI.Brain;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class BrainContentDto
{
    [Id(0)] public byte[] Content { get; }
    [Id(2)] public BrainContentType Type { get; }
    [Id(3)] public string Name { get; }

    public BrainContentDto(string name, BrainContentType contentType, byte[] content)
    {
        Name = name;
        Type = contentType;
        Content = content;
    }

    public BrainContentDto(string name, string content)
    {
        Name = name;
        Type = BrainContentType.String;
        Content = BrainContent.ConvertStringToBytes(content);
    }

    public BrainContent ConvertToBrainContent()
    {
        return new BrainContent(Name, Type, Content);
    }
}