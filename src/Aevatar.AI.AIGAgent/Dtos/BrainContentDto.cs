using System.Linq.Dynamic.Core.Exceptions;
using Aevatar.AI.Brain;
using Orleans;
using Orleans.EventSourcing;

namespace Aevatar.AI.Dtos;

[GenerateSerializer]
public class BrainContentDto
{
    [Id(0)] public byte[]? PdfContent { get; } = null;
    [Id(1)] public string StringContent { get; } = string.Empty;
    [Id(2)] public BrainContentType Type { get; }
    [Id(3)] public string Name { get; }

    /// <summary>
    /// create pdf content
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pdfContent"></param>
    public BrainContentDto(string name, byte[]? pdfContent)
    {
        Name = name;
        Type = BrainContentType.Pdf;
        PdfContent = pdfContent;
    }

    public BrainContentDto(string name, string stringContent)
    {
        Name = name;
        Type = BrainContentType.String;
        StringContent = stringContent;
    }

    public BrainContent ConvertToBrainContent()
    {
        switch (Type)
        {
            case BrainContentType.Pdf:
                return new BrainContent(Name, PdfContent);
            case BrainContentType.String:
                return new BrainContent(Name, StringContent);
            default:
                throw new ParseException("FileDto not found BrainContentType", 0);
        }
    }
}