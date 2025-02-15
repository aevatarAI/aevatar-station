using System;
using System.Text;
using System.Threading;

namespace Aevatar.AI.Brain;

public class BrainContent
{
    public byte[]? Content { get; } = null;
    public BrainContentType Type { get; }
    public string Name { get; } = string.Empty;

    /// <summary>
    /// used for create pdf file
    /// </summary>
    /// <param name="name"></param>
    /// <param name="content"></param>
    public BrainContent(string name, byte[]? content)
    {
        Type = BrainContentType.Pdf;
        Content = content;
        Name = name;
    }

    /// <summary>
    /// used for create string content
    /// </summary>
    /// <param name="name"></param>
    /// <param name="content"></param>
    public BrainContent(string name, string content)
    {
        Type = BrainContentType.String;
        Content = Encoding.UTF8.GetBytes(content);
        Name = name;
    }
}
