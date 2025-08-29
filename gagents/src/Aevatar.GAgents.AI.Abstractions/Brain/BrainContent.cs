using System.Text;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AI.Brain;

public class BrainContent
{
    public byte[] Content { get; }
    public BrainContentType Type { get; }
    public string Name { get; } = string.Empty;

    public BrainContent(string name, BrainContentType contentType, byte[] content)
    {
        Type = contentType;
        Content = content;
        Name = name;
    }

    public static byte[] ConvertStringToBytes(string content)
    {
        return Encoding.UTF8.GetBytes(content);
    }

    public static string ConvertBytesToString(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }
}